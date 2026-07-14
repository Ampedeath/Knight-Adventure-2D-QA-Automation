# CLAUDE.md — Knight Adventure 2D

Контекст для Claude Code при роботі над цим Unity-проєктом.

## Проєкт

- Гра: **Knight Adventure 2D** — Unity 2D action-adventure (лицар, вороги-скелети, зброя, руйновані рослини)
- Оригінал: `https://github.com/tinygamesunity/Knight-Adventure-2D` (форкнуто/склоновано)
- Робочий репозиторій: `https://github.com/Ampedeath/Knight-Adventure-2D-QA-Automation.git`, гілка `main`
- Локальний шлях: `D:\All for codding\Homework general\Knight-Adventure-2D-Tests`

## Технічний стек

- **Unity Editor:** 6000.0.78f1 (Unity 6) — версія зафіксована в `ProjectSettings/ProjectVersion.txt`, не змінювати без причини
- **IDE:** JetBrains Rider (не Visual Studio) — весь код пишемо і рефакторимо через Rider
- **Мова:** C#
- **Пакети:** стандартний Unity 2D стек — `com.unity.2d.aseprite`, `com.unity.2d.tilemap.extras` (оновлені до сумісних з Unity 6 версій), NavMesh компоненти для AI
- **Тестування:** Unity Test Framework, EditMode + PlayMode тести

## Структура коду

```
Assets/
  Scripts/
    Misc/                  — FlashBlink, SelfDestoyVFX, TransparencyDetection (візуальні ефекти)
    Other/                 — KnockBack (фізична реакція на удар)
    Plants/                — DestructiblePlant, DestructiblePlantVisual (руйновані об'єкти)
    Player/                — Player, PlayerVisual
    ScriptableObjects/     — EnemySO
    Skeleton/              — EnemyAI, EnemyEntity, SkeletonVisual
    Weapon/
      Sword/               — Sword, SwordSlashVisual, SwordVisual
      ActiveWeapon.cs
    GameInput.cs
    NavMeshSurfaceManagement.cs
    Utils.cs
  Animations/, Materials/, NavMeshComponents/, PlayerInputAction/,
  Prefabs/, Scenes/, ScriptableObjects/, Settings/, Sprites/, Tilemap/
```

Класи з суфіксом `*Visual` та все в `Misc/` — це презентаційний шар (анімації, VFX), не гейплейна логіка. При зміні поведінки орієнтуватись на "логічні" класи (`Player`, `EnemyAI`, `EnemyEntity`, `Sword`, `ActiveWeapon`, `KnockBack`, `DestructiblePlant`), а не на Visual-компаньйони.

## Головне правило роботи з цим проєктом

**Не вигадуй нічого, чого немає в коді.** Перед тим як щось стверджувати про механіку, поведінку класу, назву методу/події/поля — відкрий і прочитай реальний файл. Не покладайся на назву класу чи файлу як на доказ його поведінки.

Якщо просять щось реалізувати чи протестувати, а відповідної системи в репозиторії немає (наприклад інвентар, save/load, фермерство/growth-цикли, NPC-діалоги/квести) — прямо скажи про це замість того, щоб імітувати чи вигадувати заглушку.

## Стиль і конвенції

- Дотримуватись існуючого стилю проєкту (нейминг, структура папок, ScriptableObject-патерн для конфігів типу `EnemySO`)
- Публічний API (методи, події, властивості) — пріоритетний спосіб взаємодії ззовні (у тому числі з тестів), уникати прямого читання/запису приватних полів через reflection, якщо не зазначено інше
- Групувати нові файли за геймплейною областю (combat, enemy, player, physics, destructibles), не за типом файлу
- Коментарі та назви — англійською, якщо в існуючому коді так заведено (перевірити перед додаванням)

## Тестування (коли просять писати/запускати тести)

Структура:
```
Assets/Tests/
  EditMode/
  PlayMode/
  TestHelpers/
docs/
  repository-analysis.md
  automation-feasibility.md
  regression-suite.md
  how-to-run-tests.md
```

Принципи:
- Arrange / Act / Assert
- Перевіряти через public API, events, стан компонента/об'єкта, колізії — не через приватні поля
- Тести незалежні, чистять GameObject за собою
- Без крихких timing/pixel/animation-frame перевірок
- Назви тестів — на рівні геймплею (`Enemy_TakesFatalDamage_EntersDeathState`), не `TestMethod3`
- Складні для тестування класи (синглтони, приватні залежності, сцен-залежність) — документувати як testability issue, не обходити штучно

CLI/batchmode для локального запуску:
```
Unity.exe -batchmode -projectPath "D:\All for codding\Homework general\Knight-Adventure-2D-Tests" -runTests -testPlatform PlayMode -testResults "TestResults/playmode-results.xml" -logFile "Logs/playmode.log" -quit
```
(аналогічно для `-testPlatform EditMode`)

## Компіляція — відомий стан

0 errors. Некритичні warnings: `UniversalRenderPipelineGlobalSettings` missing types, sprite rect частково поза текстурою для `Hero_Idle_0`/`Hero_Idle_1`. Не потребують фіксу для роботи над задачами, якщо не сказано інше.

## Формат відповідей

1. Коротко назви, що саме зараз робиш (аналіз коду / рефакторинг / нові фічі / тести / документація).
2. Показуй реальний код із файлів проєкту, а не абстрактні приклади.
3. Якщо чогось немає в репозиторії — скажи прямо, не імітуй.
4. Якщо клас/механіка складна для зміни чи тестування — поясни чому (коротко).
5. Після завершення значного шматка роботи — короткий підсумок "що зроблено / що залишилось".