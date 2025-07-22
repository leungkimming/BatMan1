SET(SOURCES ui_Home.c
    ui_BatMan.c
    ui_ResetDaily.c
    ui_BatteryMax.c
    ui.c
    ui_comp_hook.c
    ui_helpers.c
    ui_HomeLD.c
    ui_BatManLD.c
    ui_ResetDailyLD.c
    ui_BatteryMaxLD.c)

add_library(ui ${SOURCES})
