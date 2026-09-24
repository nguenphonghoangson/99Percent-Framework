# NinetyNine Framework — Architecture

## 1. Hai tầng: `Packages/` và `Game/`

| Tầng | Chứa gì | Quy tắc |
|---|---|---|
| `Assets/Packages/<Package>/Runtime/<Module>` | Code dùng lại được: module và logic meta feature (service, config type, domain rule, presenter) | Không có prefab, asset hay string riêng của game. Mỗi thư mục con là một asmdef |
| `Assets/Game/` | Mọi thứ của riêng game này: game feature, view + prefab, content asset, scene, editor tool, test | Được phép phụ thuộc mọi package. Package không bao giờ phụ thuộc `Game/` |

**Module**: capability độc lập, sở hữu state, expose qua `I*Service`, không có UI, luôn được install (`IModule`).

**Feature**: chức năng người chơi nhìn thấy, ghép từ nhiều module (`IFeature`). Có thể tắt bằng `IFeatureToggles`, khi đó feature không được install. Feature **không phụ thuộc feature khác**, riêng Home là ngoại lệ (xem §4).

Một feature có thể nằm ở hai chỗ:
- Logic tái sử dụng được (Shop, DailyReward) → `Packages/Economy/Runtime/<Feature>`.
- View MonoBehaviour + prefab của game → `Game/Features/<Feature>/UI`.
- Feature chỉ có ý nghĩa với game này (Home, Gameplay loop, Profile rules) → `Game/Features/<Feature>/Runtime`.

## 2. Cấu trúc

```
Assets/
├── Packages/
│   ├── Core/Runtime/              NinetyNine.Core (engine-free, 1 asmdef)
│   │   ├── Module/                IModule, IFeature, ModuleHost, CoreModule, IFeatureToggles
│   │   ├── ServiceLocator/        ServiceContainer, ServiceLocator.Current
│   │   ├── Event/                 IEventBus
│   │   ├── Time/                  ITimeService (System / Manual)
│   │   ├── Serialization/         ISerializer
│   │   ├── ObjectPooling/         IObjectPool<T>, ObjectPool<T>, IPoolable (namespace NinetyNine.Core.Pooling)
│   │   ├── Scene/                 ISceneLoader
│   │   │   └── Unity/             SceneModule, UnitySceneLoader (NinetyNine.Core.Scene.Unity)
│   │   └── Utilities/             Result, CounterTable
│   ├── Persistence/Runtime/Save/  ISaveService, SaveModule, InMemorySaveProvider (engine-free)
│   │   └── Unity/                 PlayerPrefsSaveProvider, JsonUtilitySerializer
│   ├── Player/Runtime/            Inventory, Progression, Profile, Unlock
│   ├── Economy/Runtime/           Economy, Reward (module) · Shop, DailyReward (feature logic)
│   ├── Gameplay/Runtime/          Board, Move, Rule, Objective, Turn, Level, Puzzle (+Result)
│   ├── Monetization/Runtime/IAP/  IIapService, IapModule, FakeIapProvider
│   └── UI/Runtime/                NinetyNine.UI (1 asmdef)
│       ├── UIManager/             UIRoot + IUILayers (Screens / Hud / Popups / Overlay), UIModule
│       ├── Navigator/             INavigator, Navigator, UIViewOpened/ClosedEvent
│       ├── Screen/                UIView (lifecycle), UIScreen
│       ├── Popup/                 UIPopup (result, backdrop, closeOnBack)
│       ├── Factory/               UICatalog (key → prefab), IUIFactory, PrefabUIFactory, UIIconSet (id → sprite)
│       ├── Pooling/               UIInstanceCache (keep-alive instances)
│       ├── Transition/            UITransition, FadeTransition, ScalePopTransition
│       └── Input/                 BackButtonListener, InputBlocker
│
└── Game/
    ├── Bootstrap/                 GameBootstrap — composition root duy nhất, nằm trong Intro scene
    ├── Flow/                      GameScenes (tên scene), SceneEntry (NinetyNine.Game.Flow)
    ├── Features/
    │   ├── Home/UI/               HomeScreenView, MainHudView + HomeScreen/MainHud.prefab
    │   ├── Gameplay/Runtime/      PuzzleGameplay feature (level loop, revive, first-clear, LevelEndFlow)
    │   ├── Gameplay/UI/           GameplayScreen (HUD + flow), BoardView, TilePalette + GameplayScreen.prefab
    │   ├── Result/UI/             WinPopup, LosePopup + prefabs
    │   ├── Profile/Runtime/       Profile feature (avatar/title unlock, rename cost), ProfilePresenter
    │   ├── Profile/UI/            ProfilePopup, ProfileAvatarOption + ProfilePopup.prefab
    │   ├── Shop/UI/               ShopScreen, ShopProductItem + ShopScreen/ShopItem.prefab
    │   └── DailyReward/UI/        DailyRewardPopup, DayCellView + prefabs
    ├── Content/                   Levels/ Shop/ Economy/ DailyReward/ Profile/ Gameplay/ UI/ (*.asset; UI/ = UICatalog, HomeUICatalog, GameplayUICatalog, UIIconSet)
    ├── Art/UI/                    Generated/ (placeholder sprite từ UiArt), Fonts/
    ├── Art/_ThirdPartyReference/  PixelFlow/ (sprite + font LilitaOne tham chiếu, KHÔNG ship — xem README trong thư mục)
    ├── Scenes/                    Intro → Home → Gameplay (xem §5)
    ├── Editor/                    NinetyNine/Setup menus (FrameworkSetup, UiSetup, UiRebuild) + UiCatalogSplit, SceneSetupGuard,
    │                              UiSkin, UiKit, UiArt, ReferenceArtImport, ThirdPartyArtBuildGuard, UiShowcaseContent
    └── Tests/                     EditMode + PlayMode (Intro → Home → shop/daily/profile, Home ⇄ Gameplay), Fixtures (test views)
```

Package trong layout nhưng **chưa có code**: `Network`, `Competitive`, `LiveOps`, `Analytics`, `Platform`, cùng các module Account, Collection, Mission, Achievement, Booster, OfflineSync, CloudSave, Ads, Subscription, AdReward. Thư mục rỗng không được tạo sẵn: một thư mục được tạo cùng với module đầu tiên của nó.

## 3. Dependency

Phụ thuộc giữa các asmdef, chiều mũi tên là "cần":

```
Game/Bootstrap ─► mọi package + Game features + Game/Flow
Game/Features/Home ─► Shop, DailyReward, Profile, PuzzleGameplay (service), Economy, Progression, UI, Game/Flow
Game/Features/*/UI ─► logic của chính feature đó, UI (+ Reward/Economy cho DTO); Gameplay/UI thêm Game/Flow
Game/Flow           ─► Core, UI
UI                  ─► Core
Core.Scene.Unity    ─► Core

Economy/Shop        ─► Economy, Reward, Unlock, [IAP optional]
Economy/DailyReward ─► Reward, Unlock, Core.Time
Economy/Reward      ─► Economy, Player/Inventory
Player/Unlock       ─► Inventory, Progression
Gameplay/Puzzle     ─► Level ─► Objective ─► Rule ─► Move ─► Board ;  Turn
mọi module có state ─► Persistence/Save ─► Core
```

Ở cấp package, graph hiện tại là `Economy → Player, Monetization, Persistence, Core`, `Player, Monetization → Persistence → Core` và `Gameplay → Core`. Nó không có vòng, nên mỗi package có thể tách ra thành UPM package sau này.

**Cẩn thận khi thêm AdReward**: `Monetization/AdReward` cần `Reward` (ở Economy), trong khi `Economy/Shop` đã cần `Monetization/IAP`. Như vậy sẽ thành vòng `Economy ⇄ Monetization` ở cấp package. Có hai cách:
- Đặt AdReward vào Economy.
- Tách `Reward` xuống package thấp hơn.

Asmdef không báo lỗi vì vòng này chỉ tồn tại ở cấp thư mục, nên phải tự giữ.

Board, Move, Rule, Objective, Turn, Puzzle, Core, Persistence.Save, Inventory, Progression, Profile, Reward và IAP đều `noEngineReferences: true`, tức pure C#. Chúng test được không cần scene và dùng lại được phía server.

## 4. UI: Navigator, Screen, Popup

- **Mở bằng key**: `INavigator.PushScreen("shop")`, `ShowPopup("daily_reward")`. Key theo quy ước là feature id (`ShopFeature.Id`). `UICatalog` (`Game/Content/UI`) map key sang prefab, nên caller không reference view type của feature khác.
- **UI theo scene**: có hai tầng catalog. Catalog toàn cục đưa vào `UIModule` (ở Intro) giữ cấu hình canvas và view dùng ở mọi scene. Mỗi context scene có catalog riêng (Home UI: home, shop, daily_reward, profile; Gameplay UI: puzzle_gameplay, result_win, result_lose), gắn vào `SceneEntry.uiCatalog`. `IUIFactory.SetContext` tra key ở catalog của scene trước, rồi mới tới catalog toàn cục. Khi đổi scene, instance idle của scene cũ bị huỷ, còn view của scene cũ đang mở thì bị huỷ khi release thay vì được cache. Vì vậy UI của Home không chiếm bộ nhớ trong Gameplay, và ngược lại. Mỗi key chỉ nằm trong một catalog. `SceneEntry.uiCatalog` để trống thì mọi key đều lấy từ catalog toàn cục. Catalog nằm ở `Game/Content/UI/`: `UICatalog` (toàn cục), `HomeUICatalog`, `GameplayUICatalog`. `UiSetup` vẫn ghi mọi key sinh ra vào catalog toàn cục. Sau đó `UiCatalogSplit` (chạy trong `Create All` và mỗi lần bấm Play qua `SceneSetupGuard`) copy key sang catalog của scene, gắn catalog vào `SceneEntry`, rồi mới xoá key khỏi catalog toàn cục. Scene nào chưa gắn được thì key vẫn ở lại catalog toàn cục, nên game không lúc nào thiếu màn hình.
- **Screen stack**: chỉ screen trên cùng active. Screen bên dưới bị tắt, nhận `OnCovered`/`OnRevealed` nhưng vẫn giữ vị trí trong stack. `PushScreen` đóng mọi popup trước. `ReplaceScreen` đổi screen trên cùng mà không làm stack dài thêm; `SceneEntry` dùng `PopToRoot` + `ReplaceScreen` để mỗi scene bắt đầu với đúng một root screen. Screen root thì không pop được.
- **Popup stack**: nằm trên mọi screen. `ShowPopupAndWait` trả về kết quả truyền vào `Close(result)` (dùng cho confirm dialog). `closeOnBack` và `closeOnBackdrop` cấu hình trên prefab.
- **Back** (Android back/Escape): đóng popup trên cùng. Popup cấm back vẫn nuốt phím. Nếu không có popup thì pop screen. Ở root, `HandleBack` trả `false` để game tự quyết (hiện quit dialog, move task to back).
- **`IsBusy`** đúng khi còn bất kỳ thao tác nào đang chờ trong hàng hoặc đang chạy, không chỉ thao tác đang có transition. Input blocker và phím back dựa vào nó. Sau `Dispose`, các thao tác còn lại bị cancel và không đụng vào view nữa.
- **Tuần tự**: mọi thao tác được xếp hàng và chạy hết, kể cả transition, rồi mới tới thao tác sau. Input bị chặn trong lúc chạy. Hook như `OnOpened` được phép gọi navigator, lệnh đó chỉ vào hàng đợi. Không được `await` navigator bên trong hook.
- **Lifecycle** của `UIView`: `OnCreated` (một lần mỗi instance) → `OnOpening(args)` → transition → `OnOpened` … `OnClosing` → transition → `OnClosed`. GameObject active đúng trong khoảng view đang mở, nên presenter có thể bind trong `OnEnable` và dispose trong `OnDisable`.
- **Instance**: view mới được tạo dưới một `Staging` inactive, nên Awake/OnEnable chỉ chạy khi navigator thật sự mở. `keepInstance` giữ lại instance để lần sau dùng lại (Home, Shop); tắt thì destroy khi đóng.
- **Transition** là một component trên view (`FadeTransition`, `ScalePopTransition`), chạy theo unscaled time. Ngoài play mode, hoặc khi view không có transition, view mở/đóng tức thì. Nhờ vậy EditMode test chạy navigator đồng bộ mà không cần frame.
- **Layer**: `UIRoot` có bốn layer theo thứ tự từ dưới lên: Screens, Hud, Popups, Overlay. `UIModule` đăng ký `IUILayers` để view gắn chrome không thuộc stack vào đúng layer. Hud nằm trên screen nhưng dưới popup, nên popup vẫn che được HUD.
- **MainHudView** (Home): top bar (avatar, lives, coin, settings) + bottom nav (Shop / Start / Trophy). `HomeScreenView` tạo nó một lần vào `HudLayer` trong `OnCreated`, và vì Home là `keepInstance` nên HUD sống cùng instance Home. Tab là screen của navigator với Home là root: Shop tab `PushScreen("shop")`, Start tab `PopToRoot`. HUD chỉ hiện khi key của `CurrentScreen` nằm trong `MainHudView.TabScreens` (home, shop, leaderboard), nên nó tự ẩn trong Gameplay. Entry point chưa có UI trong catalog (settings, leaderboard) hiện ở trạng thái disable.
- **UIIconSet** (`Game/Content/UI/UIIconSet.asset`): map id của design data (`coin`, `avatar_02`, `gems_small`) sang sprite. Id chưa có sprite không phải lỗi: `UIIconSet.Show` vẽ placeholder có màu suy ra từ id kèm label ngắn, nên content ship được trước khi có art.
- **Art và style của prefab sinh tự động**: builder trong `UiSetup`/`UiKit` không chọn sprite trực tiếp mà chỉ xin *role* từ `UiSkin`. Mỗi role là một `SkinPart` gồm sprite và tint. `UiSkin` lấy sprite từ reference art (`Game/Art/_ThirdPartyReference/PixelFlow`) nếu thư mục đó có trong project, không có thì lấy placeholder do `UiArt` vẽ bằng code vào `Game/Art/UI/Generated`. Vì vậy đổi skin chỉ cần thay hoặc xoá art rồi chạy `Rebuild UI Prefabs`. `UiArt` chỉ ghi file một lần, nên art team thay PNG tại chỗ (cùng tên, cùng GUID). `UiKit` gom các khối dựng (panel, nút candy, title outline) để đổi look ở một chỗ.
- **Reference art bên thứ ba**: `_ThirdPartyReference/PixelFlow` chứa sprite và font LilitaOne lấy từ PixelFlow. Chúng chỉ để dàn layout theo mock-up trong lúc chờ art của Percas; chúng ta không sở hữu chúng (README trong thư mục). `ReferenceArtImport` import PNG thành UI sprite và khôi phục viền 9-slice từ `sprites.json`. `ThirdPartyArtBuildGuard` làm **release build** fail nếu build scene hay thư mục Resources chạm tới bất kỳ asset nào trong thư mục này; development build chỉ cảnh báo. Font asset LilitaOne phải patch `m_Material` → `material` để chạy với TMP 3.0.7.
- **Độ phân giải tham chiếu**: `UICatalog` toàn cục đặt `referenceResolution` 1170x2532 (iPhone 12 Pro) với `screenMatchMode` = Expand. Chỉ catalog toàn cục điều khiển canvas; các catalog của scene bỏ qua những field này.
- **Content minh hoạ**: `UiShowcaseContent` (gọi từ `UiSetup`) thêm 6 gói coin IAP (`gold_pack_1..6`, section `gold_packs`) vào `ShopConfig` và `avatar_05..08` vào `ProfileFeatureConfig` để khớp mock-up. Nó chỉ thêm id còn thiếu, không đụng dữ liệu designer đã chỉnh.

**Gameplay → Result**: `GameplayScreen` mở popup kết thúc bằng key `LevelEndKeys.Win`/`Lose`, truyền `WinArgs`/`LoseArgs`, rồi xử lý theo `LevelEndChoice` mà popup trả về (Next, Retry, Home, Revive, GiveUp). Các DTO này nằm ở `Gameplay/Runtime/LevelEndFlow.cs`. Result chỉ trình bày contract đó, còn Gameplay không reference Result. Lose popup có hai chế độ: hết lượt (Revive/Give up, revive bị disable khi không đủ tiền) và thua hẳn (Retry/Home). Phím back đóng popup mà không kèm lựa chọn, và screen hiểu đó là hành động phụ (Give up hoặc Home). Rời màn giữa level thì session kết thúc với `Quit`.

**Home** là feature duy nhất được biết feature khác, vì việc của nó là điều hướng. Nó chỉ đọc public service (`IShopService`, `IDailyRewardService`, `IProfileFeatureService`, `IPuzzleGameplayService`), mở feature trong Home scene bằng id qua navigator, và vào Gameplay bằng `ISceneLoader.LoadAsync(GameScenes.Gameplay)`.

**Profile**: `ProfilePresenter.Save(name, avatarId)` kiểm tra avatar đã unlock trước khi đổi tên, vì đổi tên có thể tốn tiền. Một avatar bị từ chối không bao giờ để người chơi bị trừ phí đổi tên. Giá trị không đổi thì bỏ qua.

## 5. Scene và Boot

Scene không phải module. Scene chỉ chứa GameObject của một runtime context và compose feature. Business logic nằm ở feature và module sau contract. Chiều phụ thuộc: Scene → Feature → Contract → Module/Service → Provider.

```
Intro ──► Home ⇄ Gameplay
  │        │        └─ SceneEntry(rootScreen = "puzzle_gameplay") → GameplayScreen (HUD + board), Result popup
  │        └─ SceneEntry(rootScreen = "home") → HomeScreenView + MainHud, tab Shop, popup Daily/Profile
  └─ GameBootstrap: compose module/provider/feature → ServiceLocator.Current → ISceneLoader.LoadAsync("Home")
```

| Scene | Chứa | Không làm |
|---|---|---|
| **Intro** | Camera + `GameBootstrap` | Không mở UI, không tính reward, không start level |
| **Home** | Camera + `SceneEntry` | Không implement Economy/Save/Account, chỉ dùng qua contract |
| **Gameplay** | Camera + `SceneEntry` (sau này thêm board world-space, VFX, camera rig qua subclass `SceneEntry`) | Không gọi thẳng Economy/Save; thắng/thua đi qua `IPuzzleGameplayService` → Reward/Progression |

```csharp
new ModuleHost()
    .AddModule(new CoreModule(new SystemTimeService(), new EventBus(Debug.LogException), toggles))
    .AddModule(new SaveModule(new PlayerPrefsSaveProvider(), new JsonUtilitySerializer()))
    .AddModule(new UIModule(uiCatalog))
    .AddModule(new SceneModule())
    .AddModule(new EconomyModule(economyConfig)) ... .AddModule(new PuzzleModule())
    .AddFeature(new PuzzleGameplayFeature(...)) ... .AddFeature(new DailyRewardFeature(...))
    .Build();   // modules trước, feature sau; Initialize theo thứ tự đăng ký
```

- **Cái gì sống qua mọi scene**: `GameBootstrap`, toàn bộ service và `UIRoot` đều `DontDestroyOnLoad`. Scene đổi chỉ thay GameObject của context.
- **`ISceneLoader`** (Core, engine-free) đổi scene ở chế độ Single. `LoadAsync(scene, args)` gọi lúc đang load thì bị bỏ qua và trả về lần load đang chạy, nên double tap Play chỉ load một lần. `Args` giữ tham số của lần load gần nhất (ví dụ progress index để replay).
- **`SceneEntry`** (`Game/Flow`) là component mỏng trong mỗi context scene. Ở `Start` nó gọi `PopToRoot` → `IUIFactory.SetContext(uiCatalog)` → `ReplaceScreen(rootScreen, loader.Args)`. Screen của context cũ bị đóng theo lifecycle bình thường, ví dụ Gameplay `Quit` level đang dở.
- **Rời Gameplay**: nút Home, lựa chọn Home trên Win/Lose popup, hay Give up rồi Home đều gọi `LoadAsync(GameScenes.Home)`. Next/Retry/Revive ở lại trong Gameplay scene.
- **Play trực tiếp một scene trong Editor**: `SceneEntry` thấy service chưa boot thì load Intro và ghi lại scene đang mở. `GameBootstrap` load lại đúng scene đó thay vì Home.
- **Tên scene** nằm ở `GameScenes` (Intro, Home, Gameplay) và phải có trong Build Settings, Intro ở index 0. `SceneSetupGuard` (Editor) kiểm tra việc này mỗi lần bấm Play: nó tạo scene còn thiếu (additive, không đụng scene đang mở) và đăng ký lại Build Settings.
- Phím back ở root screen (Home hoặc Gameplay) hiện chưa làm gì: `HandleBack` trả `false` và game chưa quyết định hành vi.

**Setup menu** `NinetyNine/Setup/Create All` chạy theo thứ tự: config → UI prefab + `UICatalog` → Intro/Home/Gameplay scene → Build Settings (Intro, Home, Gameplay đứng đầu). Nó chỉ tạo cái còn thiếu, không overwrite. `NinetyNine/Setup/Rebuild UI Prefabs` chuyển các prefab sinh từ `UiSetup` vào Trash rồi chạy lại `Create All`. Dùng sau khi sửa builder; chỉnh sửa tay trên các prefab đó sẽ mất.

## 6. Hành vi quan trọng

- **Economy**: `initialBalance` chỉ grant một lần. Overspend bị từ chối, không clamp.
- **Reward**: validate cả bundle trước khi grant.
- **Shop**: validate reward trước khi trừ tiền. IAP grant qua `IapPurchaseCompletedEvent`, dedupe theo transaction id.
- **Puzzle**: move `NoEffect` không tốn lượt. Hoàn thành objective đúng lượt cuối tính là Win. Hết lượt thì vào `OutOfMoves` và có thể revive. Board chết thì Lose ngay.
- **Level**: `LevelDatabase` là funnel order. Vượt quá số level thì loop từ `loopStartIndex`.
- **DailyReward**: so sánh theo UTC day. `ClockBehind` chặn claim. State được persist trước khi grant.
- **Profile**: validate tên trước khi trừ phí. Vietnamese diacritics hợp lệ.

## 7. Thêm mới

**Module**: tạo `Packages/<Package>/Runtime/<Name>/` với asmdef `NinetyNine.Modules.<Name>`.
- `I<Name>Service`, `*Errors`, `*Event` là public.
- Service implementation để `internal`. Save key là `module.<name>`.
- `<Name>Module : IModule`.

**Feature tái sử dụng**: logic đặt ở `Packages/<Package>/Runtime/<Name>/`:
- `<Name>Config`, service (save key `feature.<name>`), `Domain/`, `UI/<Name>Presenter` + `I<Name>View`.
- `<Name>Feature : IFeature`.

View và prefab đặt ở `Game/Features/<Name>/UI/`, asmdef `NinetyNine.Features.<Name>.View`. Content asset đặt ở `Game/Content/<Name>/`.

**Feature riêng của game**: đặt ở `Game/Features/<Name>/{Runtime,UI}`.

## 8. Chỗ khác với Modular Architecture Spec (có chủ ý)

| Spec | Code | Lý do |
|---|---|---|
| `Economy/Contracts/` tách khỏi `Runtime/` | Contract (`I*Service`, `*Errors`, `*Event`) và implementation chung một asmdef; implementation là `internal` | Consumer không reference được concrete class, nên implementation không leak mà không cần gấp đôi số asmdef. Tách asmdef Contracts khi có module thứ hai implement cùng contract |
| `EconomyService → IEconomyProvider → Firebase/Nakama/Local` | `EconomyService → ISaveService` | Chưa có backend thứ hai cho Economy. Provider chỉ có ở chỗ đã có nhiều implementation hoặc SDK ngoài: `ISaveProvider`, `IIapProvider`, `ISerializer`, `ITimeService` |
| `ServiceLocator.Get<T>()` | `ServiceLocator.Current.Require<T>()` / `TryGet<T>()` | Tương đương. Chỉ view (MonoBehaviour) dùng locator; class C# thường nhận dependency qua constructor trong `Install` |
| `IUINavigator.Open<ShopScreen>()` | `INavigator.PushScreen("shop")` / `ShowPopup("daily_reward")` | Mở theo catalog key (feature id) nên Home không phải reference view type của feature khác |
| `PoolManager` | Không có | Một manager giữ mọi pool dễ thành God Object. Mỗi owner (board, VFX spawner) tự giữ `ObjectPool<T>` của mình; UI dùng `UIInstanceCache` riêng |
| Overlay (Loading/Toast) | Mới có `UIRoot.OverlayLayer` + input blocker | Thêm API khi feature đầu tiên cần |
| `UIManager` là service | `UIRoot` là MonoBehaviour chỉ giữ canvas/layer; mọi API nằm ở `INavigator` | Một điểm vào duy nhất cho điều hướng, root thì không có logic |
| Event tên `CurrencyChanged` | `CurrencyChangedEvent` | Hậu tố `Event` để phân biệt với DTO/state cùng tên |
| Gameplay scene chứa Board, Block, GameplayCamera | Gameplay scene mới có camera + `SceneEntry`; board vẫn là uGUI `BoardView` trong `GameplayScreen` | Board placeholder đủ cho luồng hiện tại. Khi làm board thật (world-space, VFX), dựng nó trong một subclass của `SceneEntry` và giữ HUD là screen |
