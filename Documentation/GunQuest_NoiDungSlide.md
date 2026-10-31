# GunQuest — Nội dung thuyết trình

Cập nhật mới nhất: dùng từ "nhiệm vụ" thay cho "hợp đồng"; chỉ giới thiệu 2 bản đồ (Thung lũng lá đỏ, Đèo băng giá), bỏ Camp; bỏ Shovel khỏi danh sách vũ khí (còn Pistol, Rifle, Bat). Các mục bên dưới là bản cũ, bản slide là bản đúng.

Tổng hợp từ code (Assets/Scripts), tài liệu trong thư mục Documentation và lịch sử git. Số liệu lấy trực tiếp từ project.

## 1. Giới thiệu
GunQuest: game FPS săn nhiệm vụ (contract hunting) làm bằng Unity 6 (6000.3.7f1), URP 17.3, Input System 1.18. Người chơi nhận nhiệm vụ, hạ mục tiêu, rút lui an toàn để nhận thưởng rồi nâng cấp trang bị.

## 2. Tổng quan dự án
- 59 file C# (~12.000 dòng): 41 file runtime, 18 file công cụ Editor (~5.900 dòng).
- 3 bản đồ: Camp (Scene1), Autumn Valley, Icebound Pass.
- 4 slot vũ khí (Pistol, Rifle, Bat, Shovel) + bình máu + lựu đạn.
- 18 commit trên git; giao diện hoàn toàn tiếng Việt (font TextMesh Pro SDF 2048px).

## 3. Vòng lặp gameplay
1. Nhận nhiệm vụ "Dọn sạch khu trại" ở bảng nhiệm vụ.
2. Săn quái laser (Monster1); các quái khác vẫn là mối đe dọa.
3. Mục tiêu chết → vòng xanh rút lui xuất hiện; về vòng xanh, đứng trong 2,5 m và nhấn E.
4. Nhận thưởng (200 / 300 / 350 xu tùy map), lưu một lần khi rút lui thành công. Chết thì không nhận gì.
5. Dùng xu mua nâng cấp ở bảng nhiệm vụ rồi sang nhiệm vụ tiếp theo.

## 4. Điều khiển và vũ khí
Điều khiển: WASD di chuyển, chuột nhìn, chuột trái bắn, F / chuột phải đánh cận chiến nhanh, Space nhảy, Shift chạy, Ctrl ngồi, 1–4 đổi vũ khí, R nạp đạn, E rút lui, Esc tạm dừng, Enter mở bảng nhiệm vụ.
- Pistol: băng 12 + dự trữ 60, nạp 1,4 s.
- Rifle: băng 30 + dự trữ 120, nạp 2 s, bắn tự động.
- Bat / Shovel: cận chiến, không dùng đạn, có thời gian vung (wind-up) riêng, tường chặn sát thương.
- Bình máu (tối đa 5), lựu đạn (tối đa 5): nổ sau 1,8 s, bán kính 6,5 m, sát thương 120.
- Đổi vũ khí khi đang nạp thì hủy nạp, đạn mỗi slot tách riêng; tạm dừng đóng băng cả việc nạp.

## 5. Hệ thống nhiệm vụ (ContractGame)
Máy trạng thái 6 pha: Board → Hunting → Extracting → Won / Lost, cộng Paused. Singleton ContractGame.Instance; ControlsAllowed chặn bắn/di chuyển khi đang ở bảng nhiệm vụ hoặc tạm dừng. Thua: người chơi chết, rơi xuống y < -10, hoặc mất mục tiêu. Thắng: sang map kế tiếp (nextSceneName) sau 5 giây. Retry khởi động lại nhiệm vụ nhưng giữ nâng cấp đã mua.

## 6. Cửa hàng và nâng cấp (ContractProgress)
- Sát thương: +10% mỗi cấp. Băng đạn: +2 đạn Pistol / +5 đạn Rifle mỗi cấp.
- Tối đa 5 cấp; giá = 100 × (cấp hiện tại + 1) xu.
- Đạn Pistol +30 viên: 30 xu (dự trữ tối đa 180). Đạn Rifle +60 viên: 50 xu (tối đa 360).
- Bình máu 40 xu, lựu đạn 60 xu (tối đa 5).
- Lưu JSON trong PlayerPrefs, khóa GunQuest.ContractProgress.v1; dữ liệu đọc lên được kẹp giá trị hợp lệ. Trạng thái chiến đấu và đạn hiện tại không lưu.
- Kho đồ 6 slot, phím ENTER mở cửa hàng; quái chết rơi xu vàng (GoldCoin: bắn tung, hút về người chơi).

## 7. AI quái vật
- Máy trạng thái (State pattern): PatrolState → AttackState → SearchState. Thấy người chơi → tấn công; mất dấu hơn 4 s → tìm kiếm 8 s tại vị trí cuối thấy; hết giờ → quay lại tuần tra theo waypoint.
- Cảm biến: tầm nhìn 20 m, góc 85°, chiều cao mắt 1,6 m.
- Hai kiểu tấn công: Melee và Ranged (đạn Bullet), thêm laser mắt cho Monster1 (tầm 20 m, gây sát thương mỗi 0,2 s).
- Di chuyển bằng NavMeshAgent; thanh máu trên đầu; MonsterSpawner sinh quái tại điểm spawn đã snap lên NavMesh.
- Hơn 15 prefab quái (Monster1–5, Slime, Beholder, Cactus, Mushroom...).

## 8. Ba bản đồ
- Camp (Scene1): bản đồ khởi đầu, địa hình Palmov, nhiệm vụ "Dọn sạch khu trại", thưởng 200 xu.
- Autumn Valley (Thung lũng lá đỏ): Terrain 144×144 m, 4 lớp sơn, suối + 2 cầu gỗ, 7 quái (2 chặng đầu và 2 hộ vệ) + boss laser, thưởng 300 xu.
- Icebound Pass (Đèo băng giá): Terrain 160×160 m, hồ băng đóng, 2 đường tiếp cận, 6 lính gác + mục tiêu laser, thưởng 350 xu.
- Build order: Scene1 → AutumnValley → IceboundPass; map xong tự chuyển sang map kế tiếp.

## 9. Nhân vật và góc nhìn FPS
- Nhân vật nữ Humanoid: Animator 2 lớp (thân dưới di chuyển/nhảy/lăn, thân trên bắn/nạp), 92 clip chuyển đổi.
- Rig tay FPS riêng (AnimatedFirstPersonRig): mesh cánh tay skinned, Animator riêng, IK hai xương cho vai/khuỷu/cổ tay.
- Chuyển động thủ tục: giật súng, lắc theo chuột, nhịp thở, hạ súng khi chạy, phản hồi khi tiếp đất, vung Bat/Shovel.
- PlayerWeapons là nguồn duy nhất cho đạn, sát thương và hoàn tất nạp; rig hiển thị không giữ bộ đếm riêng.

## 10. Công cụ Editor
18 script Editor, menu GunQuest: tạo map (Autumn Valley, Icebound Pass, Tutorial), dựng Animator cho người chơi / quái / cận chiến, dựng mesh tay FPS, dựng prefab vật phẩm (bình máu, lựu đạn), tạo VFX laser, dọn scene. Các trình dựng từ chối ghi đè scene/map đã có để tránh mất dữ liệu.

## 11. Kiểm thử và xác minh
Bộ kiểm tra tự động trong Play Mode ghi báo cáo + ảnh chụp: FemaleFPSValidation, MeleeValidation. Đã kiểm tra: mua nâng cấp, nạp đạn, đổi vũ khí khi nạp, tạm dừng đóng băng nạp, rút lui chỉ trả thưởng một lần, chết không nhận thưởng, NavMesh đường đi tới mục tiêu, nhân vật qua cầu không rơi xuyên. Lần chạy gần nhất: PASS, không lỗi Console.

## 12. Kiến trúc và mẫu thiết kế
- State: StateMachine/BaseState cho AI quái; Phase enum cho nhiệm vụ.
- Singleton: ContractGame.Instance.
- Observer: sự kiện EnemyHealth.Died, ContractGame.CreditsAdded.
- Data object + serialization: ContractProgress ↔ JSON.
- Builder (Editor): các lớp dựng map/animator/prefab.
- Tách trách nhiệm: PlayerMotor / PlayerLook / PlayerShoot / PlayerMelee / PlayerWeapons / PlayerHealth.

## 13. Tổng kết và hạn chế hiện có
Đã có vòng chơi hoàn chỉnh: nhiệm vụ → chiến đấu → rút lui → nâng cấp, 3 map, AI, UI tiếng Việt, lưu tiến trình. Hạn chế ghi trong tài liệu: bộ asset không có clip cận chiến cho thân ngoài (chỉ có swing thủ tục ở góc nhìn FPS); súng không có chi tiết băng đạn/khóa nòng tách rời; trạng thái chiến đấu không lưu.


## 14. Thể loại (bổ sung)
FPS săn nhiệm vụ, chơi một người: nhận nhiệm vụ, vào bản đồ, hạ mục tiêu, rút lui an toàn mới nhận thưởng. Có vũ khí cận chiến, nâng cấp dần bằng xu, đồ họa low-poly.

## 15. Cốt truyện (bản nháp do người soạn slide tự viết, không có trong code)
Lũ quái lạ tràn ra từ những khe nứt và chiếm từng vùng đất. Trạm Nhiệm Vụ thuê thợ săn dọn sạch từng nơi, trả công bằng xu. Nữ thợ săn nhận nhiệm vụ đầu tiên mà chưa biết kẻ cầm đầu là một con quái một mắt biết bắn laser.
- Chương 1: Khu trại, hạ con quái một mắt đầu tiên (200 xu).
- Chương 2: Thung lũng lá đỏ, bầy quái canh các lối qua suối (300 xu).
- Chương 3: Đèo băng giá, kẻ cầm đầu ẩn ở tiền đồn phía bắc hồ băng (350 xu).

## 16. Chức năng chính
Nhiệm vụ; 4 vũ khí; bình máu và lựu đạn; cửa hàng nâng cấp; quái vật (gần, xa, boss); xu vàng rơi từ quái; cài đặt (âm lượng tổng, âm lượng hiệu ứng, độ nhạy chuột, FOV 60-105, đảo trục Y, chất lượng, toàn màn hình); lưu tiến trình.

## 17. Các loại quái (số liệu từ prefab)
13 loại: 9 đánh gần, 3 đánh xa, 1 boss laser (Monster1). Máu 50-220; người chơi 100 máu. Quái chết rơi xu vàng (mặc định 4 đồng, mỗi đồng 10 xu).

Đánh gần (máu / sát thương mỗi đòn / tốc độ):
- Swarm09 55 / 12 / 5,2 (ra đòn mỗi 0,85 s)
- Blue Slime 75 / 15 / 3,8
- Slime 85 / 15 / 3,8
- Star Fish 95 / 18 / 4,2
- Cactus 110 / 20 / 3,0
- Chest Monster 140 / 22 / 4,8
- P01 160 / 24 / 4,6
- Turtle Shell 160 / 30 / 3,2 (ra đòn mỗi 1,6 s)
- P02 220 / 26 / 4,6

Đánh xa (máu / sát thương đạn / tầm bắn / khoảng giữ / tầm phát hiện):
- Swarm08 50 / 14 / 14 m / 9 m / 22 m
- Mushroom Angry 100 / 18 / 13 m / 8,5 m / 22 m
- Beholder 220 / 15 / 16 m / 10 m / 24 m
- Monster1 (boss) 140 / 15 / 14 m / 9 m / 24 m
- Monster2-5 (mẫu đời đầu, mặc định đánh xa) 100 / 15 / 12 m / 8 m / 18 m

## 18. Boss: Quái Mắt Laser (Monster1)
140 máu, laser tầm 20 m kéo dài 1,2 s, cứ 0,2 s gây 15 sát thương (tối đa 90 mỗi lần phóng). Khi phóng laser nó đứng yên và mắt bám theo người chơi. Là mục tiêu của nhiệm vụ; chết thì vòng xanh rút lui mở ra. Có hiệu ứng nạp năng lượng ở mắt, tia laser ba lớp, tia lửa chỗ trúng, vụ nổ khi chết.

## 19. Bố trí quái ở Autumn Valley (3 chặng)
- Chặng 1 (2 quái): Blue Slime, Swarm08.
- Chặng 2 (3 quái): P01, Chest Monster, Cactus.
- Chặng 3 (2 quái + boss): Turtle Shell, Beholder, Quái Mắt Laser.
Icebound Pass: 6 lính gác trộn Turtle Shell, Swarm08, Star Fish, Chest Monster, Cactus, Slime + boss.
