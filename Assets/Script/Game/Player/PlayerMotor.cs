using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 运动学玩家引擎（Kinematic Motor）—— 只负责一件大事：
    /// 「给你一段位移，我负责把它安全地走完」。
    ///
    /// 不含任何游戏逻辑（速度、输入、姿态一概不管），完全不依赖物理引擎的重力与反弹。
    /// 玩家本体是一个触发器胶囊：既能用于外向的几何查询，又不会被物理系统推挤。
    ///
    /// 每帧的处理顺序：
    ///   解重叠 → 竖直位移 → 地面判定 → 水平位移（含台阶） → 下坡吸附 → 再判地面
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    [DisallowMultipleComponent]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("胶囊几何")]
        [SerializeField] private float radius = 0.35f;
        [SerializeField] private float height = 1.8f;

        [Header("精度与容差")]
        [SerializeField] private float skin = 0.02f;
        [SerializeField] private int maxSlideIterations = 4;

        [Header("碰撞层")]
        [SerializeField] private LayerMask collisionMask = ~0;

        [Header("地面")]
        [SerializeField] private float groundProbe = 0.12f;
        [SerializeField] private float snapDistance = 0.45f;

        [Header("斜坡与台阶")]
        [SerializeField] private float slopeLimit = 47f;
        [SerializeField] private float stepHeight = 0.35f;

        private CapsuleCollider probe;
        private readonly Collider[] overlapBuffer = new Collider[16];
        private readonly Collider[] expandBuffer = new Collider[16];
        private readonly RaycastHit[] castBuffer = new RaycastHit[8];
        private readonly Vector3[] probeOrigins = new Vector3[5];

        private float baseHeight;
        private float currentHeight;
        private float currentRadius;
        private float centerOffset;     // 胶囊中心相对 transform 原点的纵向偏移

        // ---------------- 对外只读状态 ----------------

        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float Radius => currentRadius;
        public float Height => currentHeight;

        /// <summary>脚底相对 transform 原点的纵向偏移（站立时通常是 -0.9）</summary>
        public float FootOffsetY => centerOffset - currentHeight * 0.5f;

        /// <summary>是否贴住了垂直墙面（供上层判断）</summary>
        public bool BlockedSideways { get; private set; }

        void Awake()
        {
            probe = GetComponent<CapsuleCollider>();
            if (probe != null)
            {
                probe.isTrigger = true;      // 触发器：不参与物理阻挡，仅作为纯几何查询体
                probe.enabled = true;
            }

            baseHeight = Mathf.Max(height, radius * 2f + 0.01f);
            currentHeight = baseHeight;
            currentRadius = radius;
            centerOffset = 0f;
            SyncCollider();

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.LogWarning(
                    $"[PlayerMotor] {name} 上仍挂着刚体组件。运动学方案不使用刚体，" +
                    "它会与代码位移相互打架，请手动移除。", this);
            }
        }

        // ---------------- 外部接口 ----------------

        /// <summary>改变碰撞体尺寸（蹲/趴用）。脚底位置保持不变。</summary>
        public void SetDimensions(float newHeight, float newRadius)
        {
            newRadius = Mathf.Max(newRadius, 0.05f);
            newHeight = Mathf.Max(newHeight, newRadius * 2f + 0.01f);

            currentHeight = newHeight;
            currentRadius = newRadius;
            // 让脚底始终钉在同一处：变矮时中心同步下移
            centerOffset = (currentHeight - baseHeight) * 0.5f;
            SyncCollider();
        }

        /// <summary>
        /// 检查能否安全地撑到目标尺寸（顶头检测）。
        /// 蹲/趴想站起来时调用，头顶被压住则应该维持低姿态。
        /// </summary>
        public bool CanExpandTo(float newHeight, float newRadius)
        {
            newRadius = Mathf.Max(newRadius, 0.05f);
            newHeight = Mathf.Max(newHeight, newRadius * 2f + 0.01f);
            if (newHeight <= currentHeight + 0.001f) return true;

            float centerY = (newHeight - baseHeight) * 0.5f;
            float inner = Mathf.Max(newHeight - newRadius * 2f, 0.001f);
            Vector3 center = transform.position + new Vector3(0f, centerY, 0f);
            Vector3 bottom = center - new Vector3(0f, inner * 0.5f, 0f);
            Vector3 top = center + new Vector3(0f, inner * 0.5f, 0f);

            int count = Physics.OverlapCapsuleNonAlloc(
                bottom, top, newRadius, expandBuffer, collisionMask, QueryTriggerInteraction.Ignore);

            bool blocked = false;
            for (int i = 0; i < count; i++)
            {
                Collider other = expandBuffer[i];
                if (other == null) continue;
                if (other.transform.IsChildOf(transform)) continue;
                blocked = true;
                break;
            }

            for (int i = 0; i < count; i++)
                expandBuffer[i] = null;

            return !blocked;
        }

        public void Teleport(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            IsGrounded = false;
            GroundNormal = Vector3.up;
            EvaluateGround();
        }

        /// <summary>刷新地面状态（含法线）。位移前调用一次可拿到最新的贴地情况。</summary>
        public void EvaluateGround()
        {
            Vector3 center = transform.position + new Vector3(0f, centerOffset, 0f);
            float rayLength = currentHeight * 0.5f + groundProbe + skin;
            float spread = currentRadius * 0.7f;

            probeOrigins[0] = center;
            probeOrigins[1] = center + Vector3.right * spread;
            probeOrigins[2] = center - Vector3.right * spread;
            probeOrigins[3] = center + Vector3.forward * spread;
            probeOrigins[4] = center - Vector3.forward * spread;

            bool found = false;
            float best = float.MaxValue;
            RaycastHit bestHit = default;

            for (int i = 0; i < probeOrigins.Length; i++)
            {
                if (!Physics.Raycast(probeOrigins[i], Vector3.down, out RaycastHit hit,
                        rayLength, collisionMask, QueryTriggerInteraction.Ignore))
                    continue;
                if (hit.transform.IsChildOf(transform)) continue;
                if (!IsWalkableSlope(hit.normal)) continue;
                if (hit.distance >= best) continue;

                best = hit.distance;
                bestHit = hit;
                found = true;
            }

            if (found)
            {
                IsGrounded = true;
                GroundNormal = bestHit.normal;
            }
            else
            {
                IsGrounded = false;
                GroundNormal = Vector3.up;
            }
        }

        /// <summary>
        /// 执行一段位移。内部会处理滑动、台阶、斜坡与解重叠。
        /// </summary>
        public void Move(Vector3 motion)
        {
            if (motion.sqrMagnitude < 1e-8f)
            {
                ResolveOverlap();
                return;
            }

            ResolveOverlap();
            EvaluateGround();

            bool startedGrounded = IsGrounded;
            BlockedSideways = false;

            // 1) 纵向分量优先：先处理下落/上升与头顶碰撞
            float vertical = motion.y;
            if (Mathf.Abs(vertical) > 1e-6f)
                MoveSlide(new Vector3(0f, vertical, 0f));

            EvaluateGround();

            // 2) 水平分量：先正常滑行，被墙挡住时再尝试抬腿上台阶
            Vector3 horizontal = new Vector3(motion.x, 0f, motion.z);
            if (horizontal.sqrMagnitude > 1e-8f)
            {
                Vector3 before = transform.position;
                MoveSlide(horizontal);

                float travelled = Vector3.Distance(before, transform.position);
                BlockedSideways = travelled < horizontal.magnitude - 0.02f;

                if (BlockedSideways && startedGrounded)
                {
                    Vector3 fallback = transform.position;
                    transform.position = before;
                    if (!TryStepUp(horizontal))
                        transform.position = fallback;
                    else
                        BlockedSideways = false;
                }
            }

            // 3) 下坡时吸附地面（上升中不吸附，否则会被拽住）
            if (vertical <= 0.001f)
                SnapToGround();

            ResolveOverlap();
            EvaluateGround();
        }

        // ---------------- 几何 ----------------

        private void SyncCollider()
        {
            if (probe == null) return;
            probe.radius = currentRadius;
            probe.height = currentHeight;
            probe.center = new Vector3(0f, centerOffset, 0f);
        }

        private Vector3 CapsuleCenter(Vector3 worldPosition)
            => worldPosition + new Vector3(0f, centerOffset, 0f);

        private void GetCapsule(Vector3 worldPosition, out Vector3 bottom, out Vector3 top)
        {
            float inner = Mathf.Max(currentHeight - currentRadius * 2f, 0.001f);
            Vector3 center = CapsuleCenter(worldPosition);
            bottom = center - new Vector3(0f, inner * 0.5f, 0f);
            top = center + new Vector3(0f, inner * 0.5f, 0f);
        }

        private bool IsWalkableSlope(Vector3 normal)
            => Vector3.Angle(normal, Vector3.up) <= slopeLimit + 0.1f;

        /// <summary>胶囊扫掠，自动跳过自身及其子物体，返回最近的一次命中</summary>
        private bool CastSolid(Vector3 from, Vector3 direction, float distance, out RaycastHit result)
        {
            GetCapsule(from, out Vector3 bottom, out Vector3 top);
            int count = Physics.CapsuleCastNonAlloc(
                bottom, top, currentRadius, direction.normalized, castBuffer,
                distance + skin, collisionMask, QueryTriggerInteraction.Ignore);

            result = default;
            bool found = false;
            float best = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = castBuffer[i];
                if (hit.collider == null) continue;
                if (hit.transform.IsChildOf(transform)) continue;
                if (hit.distance < best)
                {
                    best = hit.distance;
                    result = hit;
                    found = true;
                }
            }
            return found;
        }

        // ---------------- 核心位移算法 ----------------

        /// <summary>沿碰撞面滑动的位移（经典 Collide &amp; Slide）</summary>
        private void MoveSlide(Vector3 motion)
        {
            Vector3 remaining = motion;

            for (int i = 0; i < maxSlideIterations; i++)
            {
                if (remaining.sqrMagnitude < 1e-8f) break;

                float distance = remaining.magnitude;
                Vector3 direction = remaining / distance;

                if (!CastSolid(transform.position, direction, distance, out RaycastHit hit))
                {
                    transform.position += direction * distance;
                    break;
                }

                // 已经嵌进表面、拿不到有效法线时收手，避免产生非法数值
                if (hit.normal.sqrMagnitude < 0.0001f) break;

                float travel = Mathf.Max(hit.distance - skin, 0f);
                if (travel > 0f)
                {
                    transform.position += direction * travel;
                    remaining -= direction * travel;
                }

                // 把剩余位移投影到碰撞面上（沿墙滑行 / 沿坡向上）
                remaining = Vector3.ProjectOnPlane(remaining, hit.normal);

                // 投影后如果几乎为零，说明被完全抵住
                if (remaining.sqrMagnitude < 1e-6f) break;
            }
        }

        /// <summary>抬腿上台阶：抬高 → 前进 → 落到台阶面，任一步失败就放弃</summary>
        private bool TryStepUp(Vector3 horizontal)
        {
            Vector3 saved = transform.position;
            float wanted = horizontal.magnitude;
            Vector3 direction = horizontal / wanted;

            // a) 头顶要有抬升空间
            if (CastSolid(saved, Vector3.up, stepHeight, out _))
                return false;

            Vector3 lifted = saved + Vector3.up * stepHeight;

            // b) 抬高后水平前进
            if (CastSolid(lifted, direction, wanted, out RaycastHit forwardHit))
            {
                float travel = Mathf.Max(forwardHit.distance - skin, 0f);
                if (travel < wanted * 0.5f) return false;
                lifted += direction * travel;
            }
            else
            {
                lifted += direction * wanted;
            }

            // c) 往下找落脚点
            if (!CastSolid(lifted, Vector3.down, stepHeight + skin * 2f, out RaycastHit downHit))
                return false;
            if (!IsWalkableSlope(downHit.normal))
                return false;

            float drop = Mathf.Min(Mathf.Max(downHit.distance - skin, 0f), stepHeight + skin);
            transform.position = lifted + Vector3.down * drop;
            return true;
        }

        /// <summary>下坡时把玩家吸回地面，避免「起飞」</summary>
        private void SnapToGround()
        {
            if (!IsGrounded) return;

            Vector3 center = CapsuleCenter(transform.position);
            float rayLength = currentHeight * 0.5f + snapDistance + skin;

            if (!Physics.Raycast(center, Vector3.down, out RaycastHit hit,
                    rayLength, collisionMask, QueryTriggerInteraction.Ignore))
                return;
            if (hit.transform.IsChildOf(transform)) return;
            if (!IsWalkableSlope(hit.normal)) return;

            float gap = hit.distance - currentHeight * 0.5f;   // 胶囊底到地面的空隙
            if (gap > -skin && gap <= snapDistance)
            {
                transform.position += Vector3.down * gap;
                GroundNormal = hit.normal;
            }
        }

        /// <summary>把玩家从已经嵌入的几何体里推出来</summary>
        private void ResolveOverlap()
        {
            if (probe == null) return;

            GetCapsule(transform.position, out Vector3 bottom, out Vector3 top);
            int count = Physics.OverlapCapsuleNonAlloc(
                bottom, top, currentRadius, overlapBuffer, collisionMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                if (other == null) continue;
                if (other.transform.IsChildOf(transform)) continue;

                Vector3 pushDirection;
                float pushDistance;
                bool overlapping = Physics.ComputePenetration(
                    probe, transform.position, transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out pushDirection, out pushDistance);

                if (overlapping && pushDistance > 0f)
                    transform.position += pushDirection * (pushDistance + skin * 0.5f);
            }

            for (int i = 0; i < count; i++)
                overlapBuffer[i] = null;
        }
    }
}
