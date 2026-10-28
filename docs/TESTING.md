# Проверка сборки

Нужна Unity 6000.3.4f1 и Windows Build Support. Установите `ASKICH_DEVELOPMENT=1` и выполните `Askich.Editor.BuildGame.Windows`. Для результата можно указать `ASKICH_BUILD_DIR`.

Интеграционные проверки запускаются только в диагностическом player:

```powershell
.\Askich.exe -batchmode -nographics -askich-smoke -askich-test-output C:\Temp\askich-tests.json -logFile C:\Temp\askich-tests.log
```

Проверяются механики боя, задания, переходы, покупки и сохранения. Для точных проверок тестовый код напрямую задаёт позиции и состояния; это не свидетельство полного прохождения обычным вводом. Пользовательский профиль изолирован временным файлом.

Отдельный визуальный прогон использует виртуальные устройства Input System внутри процесса игры:

```powershell
.\Askich.exe -askich-preview -askich-preview-output C:\Temp\askich-gallery -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile C:\Temp\askich-preview.log
```

Прогон идёт с обычными характеристиками и игровым вводом, не телепортирует героя и не меняет здоровье или врагов. Он снимает реальные кадры и пишет `playtest.json`. Для рендеринга требуется доступный графический рабочий стол: не запускайте этот прогон с `-nographics` или скрытым окном.

Диагностические классы исключены из обычного релиза через `UNITY_EDITOR || DEVELOPMENT_BUILD`.

Поселение: добавьте `-askich-town-preview -askich-preview-output C:\Temp\askich-town`. Сценарий выполняет обычные команды строительства и смены дней, не добавляя ресурсы. Он сохраняет кадры и `settlement-playtest.json`. Навигация UI задаётся программно; это отдельный вид проверки, не тест кликов мышью.
