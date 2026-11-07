# TÀI LIỆU CHI TIẾT CHỨC NĂNG VÀ MÃ NGUỒN DỰ ÁN GUNQUEST
## BÁO CÁO PHÂN CÔNG KỸ THUẬT & HƯỚNG DẪN THUYẾT MINH BẢO VỆ ĐỒ ÁN

---

### BẢNG TỔNG QUAN PHÂN CHIA TRÁCH NHIỆM THÀNH VIÊN (CHIA ĐỀU 33.33%)

| STT | Họ và tên | MSSV | Vai trò kỹ thuật chính | Phân hệ phụ trách (Modules) | Tỷ lệ đóng góp |
|:---:|:---|:---:|:---|:---|:---:|
| **1** | **Phan Khánh Vương** | **2312802** | Trưởng nhóm / Lập trình Gameplay & Combat | Player Controller, Kho đồ 5 Slot (Súng, Cận chiến, Vật phẩm), Hệ thống Chiến đấu VFX/SFX, Quản trị Git & Kiến trúc mã nguồn | **33.33%** |
| **2** | **Phùng Nguyễn Hoài Bo** | **2312585** | Lập trình AI & Thiết kế Màn chơi | Trí tuệ nhân tạo quái vật (Enemy AI FSM), Cơ chế Đạn & Laser quái, Thanh máu Billboard, Thiết kế & Nướng NavMesh 2 Bản đồ | **33.33%** |
| **3** | **Nguyễn Ngọc Trường Dân** | **2312590** | Lập trình Hệ thống & Giao diện Game | Vòng lặp Game Loop & Quản lý Nhiệm vụ, Kinh tế & Nâng cấp (Shop, Tiền xu rơi), Pixel UI/HUD/Settings/About, Công cụ Editor | **33.33%** |

---

# PHẦN 1: PHAN KHÁNH VƯƠNG (MSSV: 2312802)
### PHÂN HỆ: PLAYER CONTROLLER, 5-SLOT WEAPONS & INVENTORY, COMBAT VFX/SFX, KIẾN TRÚC MÃ NGUỒN

---

## 1.1. Điều khiển nhân vật, di chuyển & Đồng bộ Animation (`PlayerMotor.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/PlayerMotor.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/PlayerMotor.cs)
- **Class đảm nhiệm**: `public class PlayerMotor : MonoBehaviour` (Require: `CharacterController`)
- **Cơ chế hoạt động**:
  - Quản lý vận tốc vật lý thông qua `CharacterController.Move()`, tính toán vector di chuyển theo hướng nhìn phẳng (horizontal plane) của Camera để người chơi tiến/lùi/ngang chuẩn FPS.
  - Xử lý gia tốc trọng lực `gravity = -20f`, độ nảy khi tiếp đất (`isGrounded`), và công thức vận tốc nhảy: $v = \sqrt{2 \cdot g \cdot h}$.
  - Cơ chế chuyển đổi tư thế ngồi (Crouch) thay đổi mượt mà chiều cao của `CharacterController.height` từ 2.0m xuống 1.0m bằng `Mathf.MoveTowards`.
  - Cập nhật các thông số Blend Tree của `Animator` (MoveX, MoveY, Speed, IsMoving, IsSprinting, IsCrouching) bằng mã băm `StringToHash` để tối ưu hiệu năng.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public void ProcessMove(Vector2 input)`:
     - **Tác dụng**: Nhận vector input `(WASD)` từ Input System, chiếu lên hệ tọa độ của Camera bằng hàm `GetMoveDirection`, tính toán tốc độ tương ứng (chạy bình thường `5f` hoặc chạy nhanh `8f`), cộng trọng lực theo trục Y và gọi `controller.Move()`. Đồng thời cập nhật trạng thái hoạt ảnh cho Animator.
  2. `public void Jump()`:
     - **Tác dụng**: Kiểm tra điều kiện tiếp đất (`controller.isGrounded`), không bị ngồi và chưa chết. Thiết lập vận tốc dọc `verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity)` và kích hoạt trigger `Jump` trong Animator.
  3. `public void Crouch()`:
     - **Tác dụng**: Bật/tắt trạng thái ngồi `crouching = !crouching`, truyền biến boolean vào Animator để chuyển bộ blend tree hạ thấp trọng tâm.
  4. `public void SetSprinting(bool value)`:
     - **Tác dụng**: Kích hoạt chế độ chạy nhanh `sprintSpeed = 8f` khi người chơi giữ phím Shift và đang di chuyển về phía trước, cập nhật biến `IsSprinting`.
  5. `public void SetAiming(bool value)` & `public void SetFiring(bool value)`:
     - **Tác dụng**: Thiết lập cờ nhắm bắn và nổ súng, hỗ trợ đồng bộ cánh tay và tư thế cơ thể nhân vật khi khai hỏa.
  6. `public void FaceCameraDirection()`:
     - **Tác dụng**: Ép xoay phần thân người chơi khớp với hướng nhìn ngang của Camera chính ngay khi người chơi bấm bắn hoặc vung vũ khí.
  7. `public void Reload()`, `public void PlayMeleeAnimation()`, `public void PlayHitAnimation()`, `public void PlayDieAnimation()`:
     - **Tác dụng**: Kích hoạt các trigger hoạt ảnh tương ứng (nạp đạn, vung cận chiến, trúng đòn chao đảo, và gục ngã khi hết máu).
  8. `private static Vector3 GetMoveDirection(Vector2 input)`:
     - **Tác dụng**: Chiếu vector di chuyển phẳng lên mặt đất vuông góc với vector `Vector3.up`, chuẩn hóa vector để không bị đi nhanh hơn khi bấm đi chéo `(W+D)`.
  9. `private void UpdateAnimator(Vector2 input)`:
     - **Tác dụng**: Damping chuyển động của các float parameter (`MoveX`, `MoveY`, `Speed`) với thời gian mượt `animationDampTime = 0.1f` tránh giật hình.

---

## 1.2. Xoay góc nhìn chuột FPS & Giảm chấn Recoil (`PlayerLook.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/PlayerLook.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/PlayerLook.cs)
- **Class đảm nhiệm**: `public class PlayerLook : MonoBehaviour`
- **Cơ chế hoạt động**:
  - Nhận tín hiệu delta di chuyển chuột mỗi khung hình, nhân với hệ số nhạy `mouseSensitivity` lấy từ `GameSettings`.
  - Giới hạn góc nhìn dọc (pitch) trong khoảng an toàn $[-85^\circ, +85^\circ]$ để tránh hiện tượng lật ngược camera (Gimbal lock).
  - Tích hợp hàm tiếp nhận lực giật súng (`ApplyRecoil`) đẩy camera lên trên và lệch ngang ngẫu nhiên khi bắn súng liên thanh.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public void ProcessLook(Vector2 input)`:
     - **Tác dụng**: Kiểm tra `ContractGame.ControlsAllowed`. Cập nhật góc quay ngang `yaw += input.x * sx` và xoay thân nhân vật `Quaternion.Euler(0, yaw, 0)`. Tính toán góc nâng hạ mắt `pitch = Mathf.Clamp(pitch + lookY, -85f, 85f)`.
  2. `public void ApplyRecoil(Vector2 recoil)`:
     - **Tác dụng**: Nhận vector độ giật súng từ module bắn súng, lập tức bù góc `pitch` và `yaw`, tạo cảm giác đầm tay chân thực khi bắn súng trường.

---

## 1.3. Quản lý Máu người chơi & Hiệu ứng Sát thương Màn hình đỏ (`PlayerHealth.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/PlayerHealth.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/PlayerHealth.cs)
- **Class đảm nhiệm**: `public class PlayerHealth : MonoBehaviour`
- **Cơ chế hoạt động**:
  - Theo dõi lượng máu tối đa `maxHealth = 100f` và máu hiện tại `health`.
  - Cơ chế thanh máu kép (Two-pass health bar): thanh máu trước (front bar) tụt ngay lập tức, thanh máu sau (back bar - chip health) trễ lại và hạ từ từ theo tốc độ `chipSpeed = 2f` tạo hiệu ứng thị giác mượt mà kiểu game đối kháng/FPS AAA.
  - Hiệu ứng màn hình đỏ nháy lên (`overlay.alpha = 0.8f`) khi trúng đòn rồi mờ dần theo thời gian (`fadeSpeed`).

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public void TakeDamage(float damage)`:
     - **Tác dụng**: Trừ máu người chơi, bật màn hình đỏ cảnh báo nguy hiểm, kích hoạt animation trúng đòn (`motor.PlayHitAnimation()`). Nếu máu về 0 thì gọi `motor.PlayDieAnimation()`.
  2. `public void RestoreHealth(float amount)`:
     - **Tác dụng**: Hồi lượng máu tương ứng khi uống bình máu, không vượt quá `maxHealth`.
  3. `public float GetCurrentHealth()`:
     - **Tác dụng**: Trả về số máu hiện tại cho HUD và ContractGame kiểm tra điều kiện thua cuộc.
  4. `public void UpdateHealthUI()`:
     - **Tác dụng**: Cập nhật giá trị `fillAmount` của các thanh máu UI mỗi frame.

---

## 1.4. Hệ thống Quản lý 5 Slot Vũ khí & Vật phẩm Quân nhu (`PlayerWeapons.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/PlayerWeapons.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/PlayerWeapons.cs)
- **Class đảm nhiệm**: `public class PlayerWeapons : MonoBehaviour`
- **Cơ chế hoạt động**:
  - Quản lý cấu trúc 5 Slot độc lập:
    - **Slot 1**: Súng lục (Pistol) — Bán tự động, băng đạn 12 viên, nạp 1.4s.
    - **Slot 2**: Súng trường (Rifle) — Bắn tự động liên thanh, băng đạn 30 viên, nạp 2.0s.
    - **Slot 3**: Gậy bóng chày (Bat) — Vũ khí cận chiến, không tốn đạn, sát thương diện hẹp.
    - **Slot 4**: Bình máu sinh lực (Health Potion) — Hồi 50 HP, tối đa mang 5 bình.
    - **Slot 5**: Lựu đạn nổ ném tay (Grenade) — Nổ diện rộng AoE 120 damage, tối đa mang 5 quả.
  - Tách bạch hoàn toàn giữa số đạn trong băng (`magazines[]`) và số đạn dự trữ (`reserves[]`).
  - Hỗ trợ đổi vũ khí hủy nạp đạn (`CancelReload`), tạm dừng game đóng băng nạp đạn, cập nhật thông số từ nâng cấp bảng tiến trình (`ApplyProgress`).

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public bool Equip(int index)`:
     - **Tác dụng**: Rút vũ khí tại slot chỉ định (phím 1-5), ẩn mô hình 3D của các vũ khí khác, hủy nạp đạn cũ và cấu hình thông số sát thương, tầm bắn vào component `PlayerShoot` hoặc `PlayerMelee`.
  2. `public bool Attack()`:
     - **Tác dụng**: Xử lý logic khi người chơi click chuột trái:
       - Nếu là súng (`SlotKind.Gun`): Kiểm tra đạn trong băng; nếu còn thì gọi `shoot.Shoot()` và trừ 1 viên; nếu hết thì tự động gọi `Reload()`.
       - Nếu là cận chiến (`SlotKind.Melee`): Gọi `melee.Attack()` và kích hoạt animation chém/vung.
       - Nếu là bình máu (`SlotKind.Consumable`): Gọi `UseConsumable()`.
       - Nếu là lựu đạn (`SlotKind.Throwable`): Gọi `ThrowItem()`.
  3. `private bool UseConsumable()`:
     - **Tác dụng**: Kiểm tra số lượng bình máu $>0$ và người chơi chưa đầy máu. Hồi 50 HP, trừ 1 bình trong kho đồ, sinh VFX ánh sáng xanh lá xung quanh nhân vật (`SpawnHealVfx()`), phát âm thanh chime hồi phục (`PlayHealSound()`), hiển thị popup text "+50 HP".
  4. `private bool ThrowItem()`:
     - **Tác dụng**: Kiểm tra số lựu đạn $>0$. Trừ 1 quả, tính toán tọa độ ném xuất phát từ tầm mắt Camera, truyền vận tốc ném gồm vector nhìn tới `camFwd * 18f` cộng với vector nảy lên `Vector3.up * 3.5f` và gia tốc quán tính chạy của nhân vật. Khởi tạo prefab `ThrownGrenade.prefab` gắn component `GrenadeProjectile`.
  5. `public bool Reload()` & `private IEnumerator ReloadMagazine(int index, float duration)`:
     - **Tác dụng**: Bắt đầu Coroutine nạp đạn trong thời gian quy định (1.4s súng lục, 2.0s súng trường). Khi hết thời gian, tính toán số đạn thiếu cần bù từ kho dự trữ `reserves` vào `magazines`.
  6. `public bool QuickMelee()`:
     - **Tác dụng**: Cho phép người chơi nhấn phím `F` hoặc chuột phải để tung đòn đánh cận chiến nhanh ngay cả khi đang cầm súng lục hay súng trường.
  7. `public void ApplyProgress(ContractProgress value)`:
     - **Tác dụng**: Đồng bộ cấp độ sát thương, băng đạn và số lượng vật phẩm quân nhu (Bình máu, Lựu đạn, Đạn dự trữ) từ tiến trình lưu trữ sang nhân vật và cập nhật HUD.

---

## 1.5. Cơ chế Bắn súng Hitscan & Hiệu ứng Chiến đấu (`PlayerShoot.cs`, `WeaponEffects.cs`)
- **Vị trí file mã nguồn**:
  - [`Assets/Scripts/PlayerShoot.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/PlayerShoot.cs)
  - [`Assets/Scripts/WeaponEffects.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/WeaponEffects.cs)
- **Class đảm nhiệm**: `PlayerShoot`, `WeaponEffects`
- **Cơ chế hoạt động**:
  - Áp dụng kỹ thuật Hitscan Raycasting hiệu năng cao thông qua `Physics.RaycastNonAlloc` từ trung tâm Camera tới khoảng cách `aimDistance = 100f` với layer mask để không sinh rác bộ nhớ (Zero Garbage Collection allocation).
  - Tự động bỏ qua collider của bản thân người chơi, lấy `EnemyHealth` ở mục tiêu gần nhất trúng đạn và trừ máu.
  - Kích hoạt đồng thời 3 tầng hiệu ứng âm thanh - thị giác:
    1. Chớp lửa nòng súng (Muzzle Flash VFX).
    2. Vệt đạn tracer phát sáng xé gió bay tới đích (Bullet Tracer Trail).
    3. Tia lửa va chạm và bụi đất tại điểm chạm (Impact Spark VFX).

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public bool Shoot()`:
     - **Tác dụng**: Kiểm tra nhịp bắn `fireRate` và quyền điều khiển. Thực hiện tia raycast tìm điểm trúng, trừ máu quái `enemy.TakeDamage(damage, aimPoint)` và gọi `effects.Play()`.
  2. `private Vector3 GetAimPoint(...)`:
     - **Tác dụng**: Bắn tia thẳng từ tâm màn hình góc nhìn thứ nhất, quét qua mảng `hits[]`, trả về tọa độ va chạm thực tế trong không gian 3D và vector pháp tuyến bề mặt để gắn vết đạn.
  3. `public void Play(Transform muzzle, Vector3 hitPoint, Vector3 hitNormal, bool hitSomething)`:
     - **Tác dụng**: Phát âm thanh súng nổ (Sound pool), kích hoạt hạt lửa đầu nòng và sinh đường đạn line renderer bay từ nòng súng đến `hitPoint`.

---

## 1.6. Cơ chế Tấn công Cận chiến Melee Raycast Chặn Tường (`PlayerMelee.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/PlayerMelee.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/PlayerMelee.cs)
- **Class đảm nhiệm**: `public class PlayerMelee : MonoBehaviour`
- **Cơ chế hoạt động**:
  - Sử dụng `Physics.SphereCastAll` với bán kính quét hình cầu `attackRadius = 0.65f` và tầm vung `strikeRange = 2.0f`.
  - Tích hợp cơ chế trễ ra đòn (Timing & Windup): Đòn đánh không gây sát thương ngay ở frame 0 mà đợi sau khoảng thời gian vung tay `timing.ImpactTime` mới tính va chạm.
  - **Cơ chế chống chém xuyên tường (Wall Blocking)**: Sắp xếp các va chạm theo thứ tự khoảng cách từ gần đến xa. Nếu gặp vật cản địa hình/tường chắn trước thì đòn đánh bị hấp thụ, ngăn chặn việc người chơi đứng sau góc tường chém xuyên qua quái.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public bool Attack(MeleeAttackTiming attackTiming)`:
     - **Tác dụng**: Bắt đầu chu kỳ vung vũ khí cận chiến, khóa trạng thái tấn công trong thời gian `timing.Duration`.
  2. `private void ApplyImpact()`:
     - **Tác dụng**: Được gọi khi hoạt ảnh vung gậy chạm đến điểm phát lực. Thực hiện SphereCast, tìm vật thể đầu tiên trúng đòn, nếu là `EnemyHealth` thì trừ sát thương và tạo hiệu ứng va đập vật lý.

---

## 1.7. Vật lý Lựu đạn Nổ diện rộng AoE (`GrenadeProjectile.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/GrenadeProjectile.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/GrenadeProjectile.cs)
- **Class đảm nhiệm**: `public class GrenadeProjectile : MonoBehaviour`
- **Cơ chế hoạt động**:
  - Được bắn đi bằng lực đẩy vật lý `Rigidbody.linearVelocity` và vận tốc xoay góc ngẫu nhiên `angularVelocity` tạo quỹ đạo lăn nảy chân thực trên địa hình dốc.
  - Ngòi nổ hẹn giờ đếm ngược `fuseTime = 1.8f`. Nếu va chạm trực tiếp với thân quái (`OnCollisionEnter`), lựu đạn kích nổ tức thì.
  - Khi phát nổ: Quét tất cả collider trong bán kính `explosionRadius = 6.5f`, tính toán sát thương diện rộng `explosionDamage = 120f`, sinh hiệu ứng hạt nổ lửa khói URP và phát âm thanh tiếng nổ bộc phá.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public void Initialize(GameObject vfxPrefab, float damage, float radius)`:
     - **Tác dụng**: Cấu hình prefab hiệu ứng, sát thương và tầm nổ khi được ném từ tay người chơi.
  2. `private void OnCollisionEnter(Collision collision)`:
     - **Tác dụng**: Bắt va chạm; kích nổ tức thì nếu chạm vào quái vật để tối đa hóa hiệu quả phục kích.
  3. `public void Detonate()`:
     - **Tác dụng**: Sinh VFX vụ nổ (`vfx_Explosion_01`), phát âm thanh bộc phá bằng `AudioSource.PlayClipAtPoint`, gọi `Physics.OverlapSphere` quét toàn bộ mục tiêu trong bán kính và trừ máu thông qua `EnemyHealth.TakeDamage()`.

---

# PHẦN 2: PHÙNG NGUYỄN HOÀI BO (MSSV: 2312585)
### PHÂN HỆ: TRÍ TUỆ NHÂN TẠO QUÁI VẬT (ENEMY AI & FSM), HỆ THỐNG MÁU & NAVMESH, THIẾT KẾ MÀN CHƠI (LEVEL DESIGN)

---

## 2.1. Máy trạng thái AI Quái vật Cận chiến & Tầm xa (`EnemyAI.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/Enemy/EnemyAI.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/Enemy/EnemyAI.cs)
- **Class đảm nhiệm**: `public class EnemyAI : MonoBehaviour` (Require: `EnemyHealth`, `NavMeshAgent`)
- **Cơ chế hoạt động**:
  - Xây dựng theo mô hình **Finite State Machine (FSM)** với 5 trạng thái chiến thuật:
    1. **Patrol / Idle**: Đứng gác hoặc tuần tra quan sát quanh lãnh địa.
    2. **Alert / Combat Detection**: Khi người chơi tiến vào cự ly phát hiện `detectionRange = 18f`, quái gầm rú cảnh báo và khóa di chuyển ngắn (`alertLockUntil`) để người chơi có thời gian phản ứng.
    3. **Combat Chase & Spacing**:
       - Với quái cận chiến (Melee): Dùng `NavMeshAgent` truy đuổi áp sát với tốc độ cao tới cự ly đánh `meleeRange = 2.0f`.
       - Với quái bắn xa (Ranged): Duy trì khoảng cách vàng tối ưu `preferredRange = 8f`. Nếu người chơi lại quá gần, quái chủ động lùi lại; nếu ở cự ly phù hợp, quái thực hiện kỹ thuật né đạn đi ngang (strafing ngẫu nhiên trái/phải).
    4. **Attack (Melee / Ranged / Eye Laser)**: Tùy theo cấu hình quái, tung đòn cào cắn, bắn đạn ma thuật hoặc quét tia laser mắt liên tục.
    5. **Die**: Hủy NavMesh, dừng hoạt ảnh và ngã gục khi hết máu.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `private void UpdateCombatMovement(float distance)`:
     - **Tác dụng**: Phân loại logic di chuyển: Quái cận chiến gọi `UpdateMeleeMovement()`; Quái tầm xa tính vector lùi hoặc vector vuông góc với hướng người chơi để strafe đảo hướng ngẫu nhiên sau mỗi $1.4 - 2.8$ giây.
  2. `private void FacePlayer()`:
     - **Tác dụng**: Dùng phép nội suy xoay `Quaternion.Slerp` xoay mượt phần thân quái hướng thẳng về phía người chơi theo vận tốc xoay `turnSpeed = 10f`.
  3. `private void UpdateEyeAim()`:
     - **Tác dụng**: Điều khiển nhãn cầu độc lập của quái mắt một tròng (Beholder/Monster1), xoay ống ngắm nòng mắt `gunBarrel` tracking theo vị trí đầu của người chơi trước khi bắn.
  4. `private void BeginAttack()`:
     - **Tác dụng**: Chọn ngẫu nhiên 1 trong 3 thế đánh (AttackIndex 0-2), kích hoạt trigger hoạt ảnh, đặt cờ trễ ra đòn `pendingShotTime` theo thời gian wind-up.
  5. `private void FirePendingShot()`:
     - **Tác dụng**: Được kích hoạt sau khi kết thúc thời gian vung đòn. Nếu là quái thường, khởi tạo viên đạn `bulletPrefab` với tốc độ bay `65f`. Nếu là quái boss laser, gọi `StartLaser()`.
  6. `private void ExecuteMeleeHit()`:
     - **Tác dụng**: Kiểm tra người chơi còn nằm trong tầm đánh không; nếu có thì trừ máu `PlayerHealth.TakeDamage(meleeDamage)` và sinh hiệu ứng tóe lửa máu đỏ (`vfx_Impact_01`).
  7. `private void StartLaser()`, `UpdateLaser()`, `StopLaser()` & `TraceLaser()`:
     - **Tác dụng**: Quét tia laser mắt liên tục trong thời gian `laserDuration = 1.2f`. Bắn Raycast mỗi 0.2s (`laserDamageInterval`), gây sát thương ngắt quãng lên người chơi và tạo hiệu ứng khói cháy tại điểm tiếp xúc.
  8. `public void OnDamaged(float damage)`:
     - **Tác dụng**: Phản xạ lập tức quay mặt về phía kẻ bắn, chuyển sang trạng thái cảnh giác chiến đấu, kích hoạt animation trúng đòn `Hit` (mỗi 4 phát trúng kích hoạt trạng thái choáng váng `Dizzy`).
  9. `public void DieNow()`:
     - **Tác dụng**: Khóa toàn bộ AI, tắt NavMeshAgent tránh cản đường người chơi, dừng phát tia laser và gọi animation tử trận.

---

## 2.2. Thanh máu quái vật Billboard UI & Sự kiện Tử trận (`EnemyHealth.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/Enemy/EnemyHealth.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/Enemy/EnemyHealth.cs)
- **Class đảm nhiệm**: `public class EnemyHealth : MonoBehaviour`
- **Cơ chế hoạt động**:
  - Quản lý lượng máu tối đa `maxHealth` của từng loài quái (từ 60 HP quái lính đến 300 HP quái boss).
  - Tự động sinh một Canvas World-space phía trên đầu quái vật với thanh máu (Health Bar) hiển thị lượng máu hiện tại.
  - Áp dụng kỹ thuật **Billboard**: Trong `LateUpdate()`, thanh máu luôn tự động xoay hướng trực diện về phía Camera chính của người chơi bất kể quái đang quay mặt đi hướng nào.
  - Quản trị sự kiện `public event Action<EnemyHealth> Died`: Báo hiệu cho hệ thống nhiệm vụ biết mục tiêu đã bị triệt hạ.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public void TakeDamage(float damage, Vector3 hitPoint)`:
     - **Tác dụng**: Trừ máu, cập nhật tỷ lệ `fillAmount` trên thanh máu UI, gọi hiệu ứng tóe máu hạt `MonsterEffects.PlayHit()`. Nếu máu cạn kiệt thì gọi `Die()`.
  2. `private void Die()`:
     - **Tác dụng**: Kích hoạt sự kiện `Died?.Invoke(this)`, ẩn thanh máu trên đầu, thông báo cho `EnemyAI.DieNow()`, và kích hoạt vòi phun tiền xu vàng thưởng `GoldCoin.SpawnBurst()`.
  3. `private void LateUpdate()`:
     - **Tác dụng**: Đảm bảo thanh máu luôn nhìn thẳng vào mắt người chơi bằng lệnh `healthBar.forward = healthBar.position - mainCamera.transform.position`.

---

## 2.3. Viên đạn Quái vật bay Vật lý (`Bullet.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/Enemy/Bullet.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/Enemy/Bullet.cs)
- **Class đảm nhiệm**: `public class Bullet : MonoBehaviour`
- **Cơ chế hoạt động**:
  - Đạn bay theo quỹ đạo thẳng trong không gian với vận tốc cấu hình từ quái (`projectileSpeed = 65f`).
  - Có cơ chế vòng đời tự hủy (`projectileLifeTime = 1.6f`) để tránh thất thoát bộ nhớ khi đạn bay trượt ra ngoài bản đồ.
  - Nhận biết va chạm với người chơi: Gọi `PlayerHealth.TakeDamage()`, tạo tia lửa va chạm và tự hủy viên đạn ngay lập tức.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public void Configure(float speed, float damage, float lifeTime)`:
     - **Tác dụng**: Nạp các tham số vận tốc, sát thương và thời gian tồn tại từ loại quái vật bắn ra.
  2. `public void SetOwner(Transform source)`:
     - **Tác dụng**: Gán cờ chủ sở hữu nhằm tránh trường hợp quái vật tự bắn trúng collider của chính mình.
  3. `private void OnCollisionEnter(Collision collision)`:
     - **Tác dụng**: Bắt va chạm với địa hình hoặc nhân vật người chơi, gây sát thương và tiêu hủy đạn.

---

## 2.4. Thiết kế Màn chơi & Nướng bản đồ Bề mặt Điều hướng NavMesh
- **Vị trí màn chơi**:
  - Bản đồ 1: [`Assets/Scenes/AutumnValley.unity`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scenes/AutumnValley.unity) (Thung lũng lá đỏ)
  - Bản đồ 2: [`Assets/Scenes/IceboundPass.unity`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scenes/IceboundPass.unity) (Đèo băng giá)
- **Công việc kỹ thuật đã thực hiện**:
  1. **Bản đồ Thung lũng lá đỏ (Autumn Valley)**:
     - Dựng địa hình Terrain kích thước $144 \times 144$ m với 4 lớp layer sơn mặt đất (đất mùn, cỏ khô, đá sỏi, lá phong rụng).
     - Thiết kế cảnh quan dòng suối uốn lượn chia cắt bản đồ, bắc 2 cây cầu gỗ chiến thuật làm điểm thắt cổ chai (chokepoints).
     - Phân bổ 7 quái vật chia thành các lớp phòng thủ: 2 quái canh cầu, 4 quái tuần tra trong thung lũng, và 1 boss laser trấn giữ khu tàn tích.
  2. **Bản đồ Đèo băng giá (Icebound Pass)**:
     - Dựng địa hình đồi núi tuyết phủ $160 \times 160$ m với mặt hồ nước đóng băng ở trung tâm.
     - Thiết kế 2 tuyến đường tiếp cận cứ điểm: Tuyến đường chính diện băng qua mặt hồ trống trải (dễ bị phát hiện) và tuyến đường hẻm núi sườn dốc (bọc cánh an toàn).
     - Bố trí 6 lính gác quái vật có khả năng bắn tầm xa và mục tiêu nhiệm vụ laser boss ở khu căn cứ tuyết.
  3. **Xây dựng & Nướng NavMesh Surface**:
     - Nướng dữ liệu điều hướng NavMesh phủ kín toàn bộ địa hình và cầu gỗ với góc dốc tối đa $45^\circ$, bước nhảy $0.5$ m.
     - Kiểm tra toàn bộ đường đi từ các điểm xuất hiện quái đến điểm spawn của người chơi, đảm bảo không có vùng đứt gãy, quái vật di chuyển bám sát mặt đất và không bị kẹt vào các tảng đá hay vật cản môi trường.

---

# PHẦN 3: NGUYỄN NGỌC TRƯỜNG DÂN (MSSV: 2312590)
### PHÂN HỆ: VÒNG LẶP GAME LOOP & NHIỆM VỤ, KINH TẾ & CỬA HÀNG NÂNG CẤP, TOÀN BỘ GIAO DIỆN PIXEL UI/HUD, CÔNG CỤ EDITOR

---

## 3.1. Quản lý Vòng lặp Gameplay & Máy trạng thái Nhiệm vụ (`ContractGame.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/ContractGame.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/ContractGame.cs)
- **Class đảm nhiệm**: `public class ContractGame : MonoBehaviour` (Design Pattern: **Singleton**)
- **Cơ chế hoạt động**:
  - Quản trị toàn bộ vòng đời của một màn chơi thông qua máy trạng thái 6 pha:
    ```mermaid
    stateDiagram-v2
        [*] --> Board: Khởi động màn chơi
        Board --> Hunting: Nhấn [BẮT ĐẦU NHIỆM VỤ]
        Hunting --> Extracting: Tiêu diệt mục tiêu chỉ định
        Extracting --> Won: Đứng trong vòng xanh & Nhấn E
        Hunting --> Lost: Người chơi chết / Rơi vực (y < -10)
        Extracting --> Lost: Người chơi chết
        Hunting --> Paused: Nhấn ESC
        Extracting --> Paused: Nhấn ESC
        Paused --> Hunting: Tiếp tục game
        Paused --> Extracting: Tiếp tục game
        Won --> [*]: Chuyển sang Map kế tiếp sau 5 giây
        Lost --> Board: Thử lại (Retry)
    ```
  - Quản lý biến tĩnh toàn cục `ControlsAllowed`: Tự động khóa toàn bộ thao tác di chuyển, bắn súng và xoay chuột khi người chơi đang mở Bảng nhiệm vụ, mở Cài đặt, hoặc Tạm dừng game.
  - Tự động phát hiện mục tiêu nhiệm vụ chết thông qua lắng nghe sự kiện `contractTarget.Died += OnTargetDied`.
  - Cơ chế đếm ngược chuyển màn chơi tự động sau 5 giây khi rút lui thắng trận (`AutoTransitionRoutine`).

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public void AcceptContract()`:
     - **Tác dụng**: Được gọi khi người chơi bấm nút "BẮT ĐẦU NHIỆM VỤ". Chuyển trạng thái sang `Hunting`, kích hoạt `Time.timeScale = 1`, ẩn con trỏ chuột và nạp thông số vũ khí đã mua vào `PlayerWeapons`.
  2. `private void OnTargetDied(EnemyHealth target)`:
     - **Tác dụng**: Lắng nghe quái boss chết; lập tức kích hoạt vòng tròn rút lui màu xanh lá tại vị trí xuất phát và chuyển pha sang `Extracting`.
  3. `public void TryExtract()`:
     - **Tác dụng**: Kiểm tra người chơi đứng trong bán kính `extractionRadius = 2.5f` của điểm rút lui và nhấn phím `E`. Cộng tiền thưởng nhiệm vụ (200 / 300 / 350 xu), tăng bộ đếm nhiệm vụ hoàn thành, lưu tiến trình và chuyển sang pha chiến thắng `Won`.
  4. `public void NextStage()`:
     - **Tác dụng**: Chuyển cảnh sang bản đồ tiếp theo (`nextSceneName`) bằng `SceneManager.LoadScene()`.
  5. `public void Fail(string reason)`:
     - **Tác dụng**: Kích hoạt pha thất bại `Lost`, ghi nhận lý do thua và hiển thị màn hình cho phép người chơi "Thử lại" hoặc "Về Bảng nhiệm vụ".
  6. `public void Pause()` & `public void Resume()`:
     - **Tác dụng**: Đóng băng thời gian `Time.timeScale = 0`, mở khóa con trỏ chuột và hiển thị menu tạm dừng.
  7. `public void Retry()` & `public void ReturnToBoard()`:
     - **Tác dụng**: Tải lại scene hiện tại để người chơi thực hiện lại nhiệm vụ nhưng vẫn bảo lưu toàn bộ số tiền và nâng cấp đã mua trong kho lưu trữ.
  8. `public void AddCredits(int amount)`:
     - **Tác dụng**: Cộng trực tiếp tiền xu vào tài khoản người chơi (khi nhặt tiền quái rơi), lưu bền vững vào đĩa và kích hoạt sự kiện `CreditsAdded` để HUD hiển thị hiệu ứng nảy số tiền.

---

## 3.2. Hệ thống Lưu trữ Tiến trình & Kinh tế Nâng cấp (`ContractProgress.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/ContractProgress.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/ContractProgress.cs)
- **Class đảm nhiệm**: `public class ContractProgress` (Serializable)
- **Cơ chế hoạt động**:
  - Lưu trữ bền vững dữ liệu người chơi vào `PlayerPrefs` dưới định dạng JSON với khóa định danh `GunQuest.ContractProgress.v1`.
  - Quản lý công thức giá nâng cấp lũy tiến theo cấp bậc: $\text{Cost} = 100 \times (\text{Cấp hiện tại} + 1)$ xu (tối đa cấp 5).
    - **Nâng cấp Sát thương**: Tăng $+10\%$ sát thương cho toàn bộ vũ khí mỗi cấp.
    - **Nâng cấp Băng đạn**: Tăng $+2$ viên súng lục, $+5$ viên súng trường mỗi cấp.
  - Quản lý giá mua quân nhu tiêu hao:
    - Đạn súng lục ($+30$ viên): 30 xu (tối đa 180 viên dự trữ).
    - Đạn súng trường ($+60$ viên): 50 xu (tối đa 360 viên dự trữ).
    - Bình máu ($+1$ bình): 40 xu (tối đa 5 bình).
    - Lựu đạn ($+1$ quả): 60 xu (tối đa 5 quả).

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public static ContractProgress Load()`:
     - **Tác dụng**: Đọc chuỗi JSON từ `PlayerPrefs`. Nếu là người chơi mới, khởi tạo các giá trị mặc định chuẩn (0 xu, 3 bình máu, 3 lựu đạn, 60 đạn súng lục, 120 đạn súng trường). Kiểm tra kẹp giá trị biên an toàn (`Mathf.Clamp`).
  2. `public void Save()`:
     - **Tác dụng**: Chuyển đối tượng C# thành chuỗi JSON bằng `JsonUtility.ToJson()` và ghi xuống ổ cứng.
  3. `public bool TryBuy(bool damage)`:
     - **Tác dụng**: Kiểm tra đủ tiền và chưa chạm trần cấp 5. Trừ tiền xu, tăng cấp bậc tương ứng và lưu đĩa.
  4. `public bool TryBuyPistolAmmo()`, `TryBuyRifleAmmo()`, `TryBuyPotion()`, `TryBuyGrenade()`:
     - **Tác dụng**: Xử lý logic mua sắm từng loại quân nhu trong Cửa Hàng, trừ tiền xu và cộng số lượng vào kho dự trữ.

---

## 3.3. Toàn bộ Hệ thống Giao diện Pixel UI, HUD, Cài đặt & Hộp thoại Nhóm (`ContractUI.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/ContractUI.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/ContractUI.cs)
- **Class đảm nhiệm**: `public class ContractUI : MonoBehaviour`
- **Cơ chế hoạt động**:
  - Dựng giao diện hoàn toàn bằng mã nguồn động (Dynamic Code UI Generation) sử dụng bộ Sprite Pixel UI cổ điển và phông chữ tiếng Việt sắc nét `TextMeshPro SDF`.
  - Tích hợp toàn diện các phân hệ giao diện:
    1. **Bảng Nhiệm Vụ (Mission Board)**: Cho phép chuyển đổi tab lựa chọn giữa các bản đồ (Thung lũng lá đỏ / Đèo băng giá), xem thông tin nhiệm vụ, tiền thưởng, và hệ thống cửa hàng 2 tab (Tab Nâng Cấp & Tab Quân Nhu).
    2. **HUD Chiến Đấu (Gameplay HUD)**: Hiển thị thanh máu kép động, la bàn cự ly rút lui, số lượng đạn trong băng / đạn dự trữ, số lượng bình máu / lựu đạn, bảng ví tiền hiển thị số xu nảy hiệu ứng popup khi nhặt.
    3. **Thanh Hotbar 5 Slot trực quan**: Hiển thị rõ ràng 5 ô vũ khí và vật phẩm (1: Súng Lục, 2: Súng Trường, 3: Gậy Bóng Chày, 4: Bình Máu, 5: Lựu Đạn), ô đang cầm được làm sáng viền vàng và phóng to nổi bật.
    4. **Bảng Cài Đặt (Settings Panel)**: Điều chỉnh mượt mà âm lượng tổng, âm thanh hiệu ứng, độ nhạy chuột, đảo trục Y, FOV, chất lượng đồ họa.
    5. **Hộp Thoại Giới Thiệu (About Dialog)**: Popup hiển thị thông tin đồ án GunQuest, danh sách 3 thành viên nhóm kèm MSSV, vai trò phân công chi tiết và phiên bản game.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public void Initialize(ContractGame contractGame, PlayerHealth playerHealth)`:
     - **Tác dụng**: Khởi tạo Canvas chính, nạp font chữ SDF, cắt lát sprite Pixel UI, đăng ký lắng nghe sự kiện ví tiền `CreditsAdded` và gọi dựng toàn bộ cây phân cấp giao diện.
  2. `private void BuildBoard()`, `BuildHUD()`, `BuildShop()`, `BuildSettingsPanel()`, `BuildAboutDialog()`:
     - **Tác dụng**: Các phương thức dựng layout tự động bằng Anchor và RectTransform, thiết lập màu sắc chuẩn Pixel Art cổ điển (Ink, Paper, Panel, Accent Gold).
  3. `public void RefreshHUD()`:
     - **Tác dụng**: Cập nhật chỉ số máu người chơi, số đạn của vũ khí đang cầm, số bình máu và lựu đạn còn lại theo thời gian thực mỗi khung hình.
  4. `public void RefreshMenu()`:
     - **Tác dụng**: Đồng bộ hóa thông tin Bảng nhiệm vụ với dữ liệu bản đồ đang chọn, cập nhật giá tiền nâng cấp và trạng thái các nút mua.
  5. `public void SelectMap(int index)`:
     - **Tác dụng**: Xử lý logic chọn bản đồ khi người chơi click tab Map 1 hoặc Map 2, tải mô tả mục tiêu và tự động chuyển scene tương ứng.
  6. `public void SetShopTab(bool supplies)`:
     - **Tác dụng**: Chuyển đổi qua lại giữa Tab Nâng cấp chỉ số trang bị và Tab Mua đạn/máu/bom.
  7. `public void ToggleSettingsFromBoard()`:
     - **Tác dụng**: Bật/tắt giao diện Cài đặt ngay từ Bảng nhiệm vụ hoặc khi đang tạm dừng trận đấu.
  8. `public void OpenAbout()` & `public void CloseAbout()`:
     - **Tác dụng**: Đóng/mở hộp thoại giới thiệu thông tin đồ án và danh sách thành viên nhóm.

---

## 3.4. Quản lý Cài đặt Cấu hình Trò chơi (`GameSettings.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/GameSettings.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/GameSettings.cs)
- **Class đảm nhiệm**: `public class GameSettings` (Design Pattern: **Singleton**)
- **Cơ chế hoạt động**:
  - Quản lý các tùy chọn thiết lập của game thủ: `masterVolume`, `sfxVolume`, `mouseSensitivity`, `invertY`, `fieldOfView`, `qualityLevel`, `fullscreen`.
  - Tự động lưu và tải cấu hình từ `PlayerPrefs`. Áp dụng ngay lập tức góc nhìn FOV cho Camera chính và cập nhật âm lượng toàn bộ game.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public static GameSettings Load()`:
     - **Tác dụng**: Đọc các khóa `GQ_MasterVol`, `GQ_Sensitivity`,... từ `PlayerPrefs`.
  2. `public void Save()`:
     - **Tác dụng**: Ghi các thông số vừa điều chỉnh xuống bộ nhớ máy.
  3. `public void ResetDefaults()`:
     - **Tác dụng**: Khôi phục các thiết lập đồ họa và điều khiển về giá trị cân bằng mặc định của nhà phát triển.

---

## 3.5. Hiệu ứng Tiền xu Rơi tự do & Hút Nam châm (`GoldCoin.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/GoldCoin.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/GoldCoin.cs)
- **Class đảm nhiệm**: `public class GoldCoin : MonoBehaviour`
- **Cơ chế hoạt động**:
  - Hoạt động theo máy trạng thái 4 pha của đồng xu:
    1. **Bursting**: Khi quái chết, tung ra chùm 3-5 đồng xu bay vồng lên không trung theo quỹ đạo góc hình nón phân tán $360^\circ$.
    2. **Landed**: Đồng xu chạm mặt đất và nảy nhẹ 2 nhịp rồi nằm chờ người chơi.
    3. **Attracting**: Sau độ trễ $1.8$ giây hoặc khi người chơi lại gần trong bán kính $5.0$ m (`magnetRange`), đồng xu tự động tăng tốc bay lướt về phía nhân vật.
    4. **Collected**: Khi chạm người chơi ở cự ly $< 0.85$ m, phát âm thanh lách cách "chime", cộng tiền xu vào `ContractGame.Instance.AddCredits()` và tiêu hủy đồng xu.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public static void SpawnBurst(Vector3 position, int count, int valuePerCoin)`:
     - **Tác dụng**: Phương thức tĩnh được gọi từ `EnemyHealth.Die()` để sinh cụm đồng xu vàng tại xác quái.
  2. `private void Update()`:
     - **Tác dụng**: Cập nhật vật lý rơi tự do, kiểm tra khoảng cách với người chơi để kích hoạt lực hút nam châm.
  3. `private void Collect()`:
     - **Tác dụng**: Cộng tiền thưởng, hiển thị popup trên HUD và phát âm thanh nhặt xu.

---

## 3.6. Công cụ Editor Tinh gọn Scene & Dọn dẹp Cây phân cấp (`SceneCleaner.cs`)
- **Vị trí file mã nguồn**: [`Assets/Scripts/Editor/SceneCleaner.cs`](file:///d:/Vuong/Nam%204/GunQuest/Assets/Scripts/Editor/SceneCleaner.cs)
- **Class đảm nhiệm**: `public static class SceneCleaner` (Unity Editor Tool)
- **Cơ chế hoạt động**:
  - Tích hợp trực tiếp vào thanh Menu của Unity Editor: `GunQuest > Clean Scene Hierarchy`.
  - Tự động mở lần lượt từng scene trong project (`AutumnValley.unity`, `IceboundPass.unity`) và các file Prefab nhân vật.
  - Quét và giải nén hoàn toàn các GameObject trung gian bị lồng rác (`NestedParent_Unpack`), chuyển các đối tượng con ra ngoài và tiêu hủy node rác.
  - Loại bỏ triệt để các phần xương thừa không sử dụng (Feet, Tail, Wing, Body rỗng) trên nhân vật nữ FPS để tối ưu hóa bộ nhớ và tốc độ xử lý của Scene.

- **Danh sách các hàm quan trọng & tác dụng cụ thể**:
  1. `public static void CleanAllScenes()`:
     - **Tác dụng**: Tự động duyệt qua toàn bộ danh sách scene và prefab của game để thực thi dọn dẹp hàng loạt và lưu lại bằng `AssetDatabase.SaveAssets()`.
  2. `public static void CleanScene(string scenePath)`:
     - **Tác dụng**: Mở một scene đơn lẻ, truy tìm các node rác và giải nén an toàn các container lồng nhau.
  3. `private static void UnpackCompletely(GameObject go)`:
     - **Tác dụng**: Gỡ bỏ triệt để liên kết Prefab Instance phức tạp để thao tác xóa node con mà không vi phạm quy tắc đóng gói của Unity.

---

# PHẦN 4: HƯỚNG DẪN THUYẾT MINH & TRẢ LỜI VẤN ĐÁP KHI BẢO VỆ ĐỒ ÁN

### Dành cho Phan Khánh Vương (Player Controller, Vũ khí & Chiến đấu):
- **Câu hỏi 1**: *Tại sao lại chọn cơ chế Hitscan Raycast cho súng thay vì bắn đạn vật lý bay?*
  - **Trả lời**: Súng ngắn và súng trường có vận tốc đầu đạn cực nhanh trong thực tế ($> 800$ m/s). Ở cự ly dưới 100m, áp dụng Hitscan Raycast Non-Alloc vừa phản hồi tức thì cho người chơi cảm giác bắn nhạy, vừa đạt hiệu năng tối ưu (Zero Garbage Collection) mà không lo đạn bay xuyên vật cản (đạn bị trôi frame). Đối với lựu đạn cần đường bay vòng cung rơi nảy thì nhóm mới áp dụng Rigidbody vật lý.
- **Câu hỏi 2**: *Cơ chế 5 slot vũ khí và hủy nạp đạn được kiểm soát như thế nào?*
  - **Trả lời**: Em viết class `PlayerWeapons` làm Single Source of Truth. Đạn trong băng và đạn dự trữ được lưu ở 2 mảng riêng biệt. Khi người chơi đang nạp đạn mà bấm đổi vũ khí hoặc vung cận chiến, hàm `CancelReload()` sẽ lập tức dừng Coroutine nạp đạn, ngăn việc gian lận đạn tự nạp khi đổi vũ khí.

### Dành cho Phùng Nguyễn Hoài Bo (Enemy AI, NavMesh & Level Design):
- **Câu hỏi 1**: *Máy trạng thái của quái vật xử lý khoảng cách chiến đấu như thế nào để không bị chồng lấn vào người chơi?*
  - **Trả lời**: Quái vật được chia thành 2 loại với thông số riêng: Quái cận chiến có cự ly dừng `stoppingDistance = range * 0.6f` để áp sát cào chém; còn quái bắn xa có khoảng cách ưu tiên `preferredRange = 8f`. Nếu người chơi tiến lại gần hơn 6.5m, quái tự động lùi lại; nếu ở cự ly thích hợp, quái thực hiện strafing đi ngang ngẫu nhiên né đạn. Toàn bộ chuyển động đều chạy trên NavMesh Surface với thuật toán tránh vật cản chất lượng cao (High Quality Obstacle Avoidance).
- **Câu hỏi 2**: *Laser của quái boss gây sát thương liên tục hay theo nhịp?*
  - **Trả lời**: Em lập trình hàm `TraceLaser()` bắn tia Raycast từ mắt quái vật. Sát thương không trừ mỗi frame (sẽ làm người chơi chết ngay tức khắc) mà được chia theo nhịp `laserDamageInterval = 0.2f`, mỗi nhịp trừ lượng máu nhất định và đi kèm hiệu ứng khói cháy tại điểm tiếp xúc, tạo áp lực cho người chơi phải tìm vật cản ẩn nấp.

### Dành cho Nguyễn Ngọc Trường Dân (Game Loop, UI/HUD, Shop & Editor):
- **Câu hỏi 1**: *Làm sao đảm bảo dữ liệu nâng cấp và tiền xu không bị mất khi người chơi thua trận hoặc chuyển bản đồ?*
  - **Trả lời**: Em thiết kế lớp `ContractProgress` tuần tự hóa lưu trữ bền vững qua `PlayerPrefs` dưới định dạng JSON. Khi người chơi rút lui thành công hoặc nhặt xu quái rơi, dữ liệu được ghi đĩa ngay. Khi màn chơi bị tải lại (Retry) hoặc chuyển sang map mới, hàm `ContractProgress.Load()` được gọi để khôi phục toàn bộ số tiền, cấp nâng cấp và số bình máu/lựu đạn đã mua.
- **Câu hỏi 2**: *Tại sao nhóm quyết định dựng UI hoàn toàn bằng code (ContractUI.cs) thay vì kéo thả Prefab Canvas thủ công?*
  - **Trả lời**: Dựng UI qua mã nguồn động đảm bảo tính độc lập tuyệt đối giữa các bản đồ (Scene-independent). Bất kỳ scene mới nào được tạo chỉ cần gắn script `ContractGame` là toàn bộ Canvas, HUD, Bảng nhiệm vụ, Cửa hàng và Menu Cài đặt tự động sinh ra chuẩn xác mà không bao giờ bị đứt gãy tham chiếu (Missing Reference) khi làm việc nhóm trên Git.
