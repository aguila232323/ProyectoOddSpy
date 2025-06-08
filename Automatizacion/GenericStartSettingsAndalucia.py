from cyandroemu import TermuxAutomation, clean_zombies
import pandas as pd
from time import sleep as timesleep
import atexit





atexit.register(clean_zombies)
parsers = TermuxAutomation.load_parsers_or_download_and_compile("g++")
df_devices = TermuxAutomation.find_suitable_devices_for_input_events()
screen_width, screen_height = TermuxAutomation.get_resolution_of_screen()
te = TermuxAutomation(
    parsers=parsers,
    mouse_device=df_devices.loc[
        (df_devices["max"].str.len() == 2) & (df_devices["type"] == "mouse")
    ]["path"].iloc[0],
    keyboard_device=df_devices.loc[
        (df_devices["keys_found"]) & (df_devices["type"] == "keyboard")
    ]["path"].iloc[0],
    mouse_device_max=df_devices.loc[
        (df_devices["max"].str.len() == 2) & (df_devices["type"] == "mouse")
    ]["max"].iloc[0][0],
    screen_height=screen_height,
    screen_width=screen_width,
)
te.sh_exe: str = "/bin/sh"
te.kill_zombies()

df = te.get_df_uiautomator2(with_screenshot=False)

Casino = "ApuestasAndalucia"
myshell = te.open_shell(
    buffer_size=40960,
    exit_command=b"exit",
    print_stdout=False,
    print_stderr=False,
)


df = pd.DataFrame()
while df.empty:
    df = te.get_df_uiautomator2(with_screenshot=False)
    df = df.loc[df["aa_text"] == Casino]

df.aa_input_tap.iloc[0]()
timesleep(20)


duration_ms = 1000
for i in range(3):
    myshell.sh_input_swipe(770, 700, 770, 292, duration_ms, timeout=10)


timesleep(2)
df = pd.DataFrame()
while df.empty:
    df = te.get_df_uiautomator2(with_screenshot=False)
    df = df.loc[df["aa_text"] == "Web Completa"]
df.aa_input_tap.iloc[0]()

timesleep(6)



duration_ms = 1000
for i in range(1):
    myshell.sh_input_swipe(770, 650, 770, 292, duration_ms, timeout=10)


df = pd.DataFrame()
while df.empty:
    df = te.get_df_uiautomator2(with_screenshot=False)
    df = df.loc[df["aa_text"] == "DEPORTES"]
df.aa_input_tap.iloc[0]()
timesleep(5)

df = pd.DataFrame()
while df.empty:
    df = te.get_df_uiautomator2(with_screenshot=False)
    df = df.loc[(df["aa_content_desc"] == "FÚTBOL FÚTBOL")]

df.aa_input_tap.iloc[0]()
timesleep(2)

df = pd.DataFrame()
df = te.get_df_uiautomator2(with_screenshot=False)

Maseventos_row = df[df["aa_text"] == "Más eventos"].iloc[0]

duration_ms = 100
while Maseventos_row["aa_visible_to_user"] == 0:
    myshell.sh_input_swipe(770, 690, 770, 292, duration_ms, timeout=10)
    df = te.get_df_uiautomator2(with_screenshot=False)
    Maseventos_row = df[df["aa_text"] == "Más eventos"].iloc[0]
    df.loc[(df["aa_text"] == "Más eventos")]

df = pd.DataFrame()
while df.empty:
    df = te.get_df_uiautomator2(with_screenshot=False)
    df = df.loc[(df["aa_text"] == "Más eventos")]
df.aa_input_tap.iloc[0]()
