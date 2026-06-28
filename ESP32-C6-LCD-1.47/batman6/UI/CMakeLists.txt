SET(SOURCES ui_BatMan.c
    ui_ResetDaily.c
    ui_BatteryMax.c
    ui_Set_Zero_Base.c
    ui.c
    ui_comp_hook.c
    ui_helpers.c)

add_library(ui ${SOURCES})
