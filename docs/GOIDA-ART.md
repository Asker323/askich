# Графика этапа GOIDA 2.2

Новые PNG созданы встроенным генератором изображений для этого проекта. Не скачаны из чужих игр. Художественная стилизация не является точной реконструкцией музейного предмета.

- `Assets/Resources/Medieval/rus-animation.png`: 1024×1536 RGBA, 24 состояния (6 рядов × 4 кадра). Исходный результат: `exec-71240cc3-d952-4419-88cb-30c127e9c4ea.png`.
- `Assets/Resources/Medieval/village-life.png`: 1447×1087 RGBA, мужчина, женщина и курица, по четыре кадра. Исходный результат: `exec-fac9ef90-4c9d-4061-b47b-261ae199b397.png`.

У обеих текстур проверен настоящий альфа-канал. RGB прозрачных пикселей атласа воина содержит коричневую подложку; она не отображается в игре. Предварительный просмотр без корректного применения альфы может показать её. PNG перенесены без ретуши. `AnimatedArt` один раз выделяет связанные силуэты по альфе внутри явно заданных прямоугольников: это не даёт соседнему копью попадать в кадр. Кадры кэшируются и загружаются до начала боя.

Запрос для воина:

> Create a production-ready TRANSPARENT PNG sprite sheet with real alpha, no background of any color. 4 columns by 6 rows, 24 separate small full-body sprites, each entirely inside its own cell, generous empty transparent margins. A single consistent medieval Rus warrior with red cloak, mail shirt, pointed steel helmet, short spear and round wooden shield, facing right/down-right, matching detailed earthy pixel-art game sprites. Row 1 idle breathing 4 frames. Row 2 walking 4 distinct leg cycle frames. Row 3 spear attack windup thrust extension recovery 4 frames. Row 4 shield block raise brace impact lower 4 frames. Row 5 damage recoil recovery 4 frames. Row 6 death stagger kneel falling lying on ground 4 frames. Keep all weapons within cell, no shadows or effects outside characters. No stage, no gradient, no brown field, no lighting backdrop. Background pixels MUST have zero alpha. Consistent character size in every cell, feet baseline consistent except death. No text labels, no borders.

Содержание запроса для жителей: атлас 4×3, прозрачный фон, спокойный сельский работник в льняной одежде, женщина в приглушённом синем платье и головном уборе, белая курица с красным гребнем; четыре кадра ходьбы/клевания, вид сверху под углом, земляная палитра и свободные поля между силуэтами. Это описание запроса, а не дословная копия.

Новые отдельные хозяйственные здания в этот выпуск не вошли: два запроса генератору завершились ошибкой соединения. В игре сохранены рабочие общие изображения из 2.1, без подмены механик макетами.

Звуки генерируются кодом `SynthAudio`: шумовые огибающие оружия, ударов, шагов, звон щита и зацикленный ветер. Музыка и записанные актёрами голоса не добавлялись.
