from cyandroemu import TermuxAutomation, clean_zombies
import pandas as pd
from time import sleep as timesleep
import atexit


atexit.register(clean_zombies)
parsers = TermuxAutomation.load_parsers_or_download_and_compile("g++")
df_devices = TermuxAutomation.find_suitable_devices_for_input_events()
print(df_devices)
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
te.kill_zombies()



while True:
    df = te.get_df_uiautomator2(with_screenshot=False)

    df_FC = df[df['aa_text'].str.contains('FC', na=False)]
    df_FC = df_FC.loc[df_FC['aa_text'].str.len().idxmin()]
    aa_indent_value = df_FC['aa_indent'] if not df_FC.empty else None
    print(aa_indent_value)


    index_ap_ultima_hora = df[df["aa_text"].str.startswith("Ap.")].index[0]
    df_filtered = df[
        (df.index >= index_ap_ultima_hora)
        & (df["aa_indent"].isin([aa_indent_value]))
        & (
            (df["aa_clazz"] == "android.view.View")
            | (df["aa_clazz"] == "android.widget.TextView")
        )
        & (~df["aa_text"].str.match(r"^\d+$", na=False))
        & (df["aa_index"].isin([0]))
        & (~df["aa_text"].isin(["X", "\ue914", ""]))
        & ~(
            (df["aa_text"].str.startswith(r"\\", na=False))
            & (df["aa_text"] != "\ue943")
        )
        & (~df["aa_text"].str.contains(r"\bISO|ID\b", na=False))
        & ~((df["aa_text"] == "\ue943") & (df["aa_text"].shift(1) == "\ue943"))
    ]

    texto = df_filtered["aa_text"].tolist()
    partidos = []
    current_match = []

    for item in texto:
        if item == "\ue943":
            if current_match:
                partidos.append(current_match)
                current_match = []
        else:
            current_match.append(item)

    columnas = ["EquipoA", "EquipoB", "Horario", "CuotaA", "Empate", "CuotaB"]
    df_partidos = pd.DataFrame(partidos, columns=columnas)

    print(df_partidos)

    df = df.loc[(df.aa_content_desc == "Refresh")]
    df.aa_input_tap.iloc[0]()
    timesleep(5)
