from cyandroemu import TermuxAutomation, clean_zombies
import pandas as pd
from time import sleep as timesleep
import atexit
import numpy as np
import re
import time

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

te.kill_zombies()

# Navegador : Mozilla
#DesktopView : True

def es_numero_decimal(texto):
    return bool(re.fullmatch(r"\d+\.\d+", str(texto)))

def es_numero_coma(texto):
    return bool(re.fullmatch(r"\d+\,\d+", str(texto)))

def es_hora_o_fecha(texto):
    return bool(
        re.fullmatch(r"\d{1,2}:\d{2}", texto)  # Formato HH:MM
        or re.fullmatch(r"Comienza en \d{1,3}′?", texto)
        or re.fullmatch(r"\d{1,2}/\d{1,2}/\d{4}", texto)  # Formato DD/MM/YYYY
        or re.fullmatch(
            r"[A-Za-z]{3} \d{1,2} [A-Za-z]{3} \d{1,2}:\d{2}", texto
        )  # Ej: "Mar 19 Mar 14:30"
        or re.fullmatch(r"(Mañana|Hoy) \d{1,2}:\d{2}", texto)  #"Hoy 14:00"
        or re.fullmatch(r"(Mañana|Hoy) / \d{1,2}:\d{2}", texto) # "Hoy / 18:30"
    )


def es_nombre_valido(texto):
    return not re.fullmatch(r"\d+(:\d+)?", texto)


def extraer_partidos(arr):
    partidos = []
    for i in range(len(arr) - 9):
        if (
            isinstance(arr[i], str)
            and es_hora_o_fecha(arr[i])  # Fecha
            and es_numero_decimal(arr[i + 1])  # Cuota A
            and es_numero_decimal(arr[i + 2])  # Cuota Empate
            and es_numero_decimal(arr[i + 3])  # Cuota B
            and es_numero_coma(arr[i + 4])  # Nº Goles
            and es_numero_decimal(arr[i + 5])  # Mas de x Goles
            and es_numero_decimal(arr[i + 6])  # Menos de x Goles
            and isinstance(arr[i + 7], str)
            and es_nombre_valido(arr[i + 7])  # Validar nombre equipo A
            and isinstance(arr[i + 8], str)
            and es_nombre_valido(arr[i + 8])  # Validar nombre equipo B
        ):
            partidos.append(
                [
                    arr[i + 7],  # Equipo A
                    arr[i + 8],  # Equipo B
                    arr[i],  # Fecha
                    arr[i + 1],  # Cuota A
                    arr[i + 2],  # Cuota Empate
                    arr[i + 3],  # Cuota B
                    arr[i + 4],  # Nº Goles
                    arr[i + 5],  # Mas de x Goles
                    arr[i + 6],  # Menos de x Goles
                ]
            )

    return pd.DataFrame(
        partidos,
        columns=[
            "EquipoA",
            "EquipoB",
            "Horario",
            "CuotaA",
            "CuotaEmpate",
            "CuotaB",
            "Nº Goles",
            "Mas de x Goles",
            "Menos de x Goles",
        ],
    )



while True:
    start_time = time.time()

    #OBTINE ELEMENTOS DE LA APP

    df = te.get_df_uiautomator2(with_screenshot=False)

    #FILTROS
    cond_name = df["aa_text"].str.contains("FC", na=False)
    cond_decimals = df["aa_text"].str.contains(r"\d+\.\d+", na=False)
    cond_fechas_horas = df["aa_text"].apply(es_hora_o_fecha)
    cond_numeros_comas = df["aa_text"].apply(es_numero_coma)

    #APLICAMOS LOS FILTROS Y COGO LA MODA
    aa_indent_values_Name = df.loc[cond_name, "aa_indent"].mode().iloc[0] if cond_name.any() else None
    aa_indent_values_Decimals = df.loc[cond_decimals, "aa_indent"].mode().iloc[0] if cond_decimals.any() else None
    aa_indent_values_FechasHoras = df.loc[cond_fechas_horas, "aa_indent"].mode().iloc[0] if cond_fechas_horas.any() else None
    aa_indent_values_NumerosGoles = df.loc[cond_numeros_comas, "aa_indent"].mode().iloc[0] if cond_numeros_comas.any() else None
    aa_indent_values = {aa_indent_values_Name, aa_indent_values_Decimals, aa_indent_values_FechasHoras, aa_indent_values_NumerosGoles} - {None}


    # FILTRO A PARTIR DE DONDE COMIENZAN LOS PARTIDOS

    indice_primer_en_juego = df[df["aa_text"].str.contains("Fútbol", na=False)].index.min()
    df_filtrado = df.loc[indice_primer_en_juego:]


    #FILTRO RESTO ELEMENTOS

    excluded_texts = {
        "",
        "Sub-20",
        "Sub-21",
        "Sub-22",
        "Sub-23",
        "Reservas",
        "Apostar ahora",
        "X",
        "1",
        "2",
        "\uea8c",
        "F",
        "CREA TU APUESTA",
    }
    allowed_clazz = {"android.view.View"}

    allowed_indexs = {1, 4, 0, 2}

    df_filtered = df[
        df["aa_index"].isin(allowed_indexs)
        & df["aa_indent"].isin(aa_indent_values)
        & (df["aa_clazz"] == "android.view.View")
        & ~df["aa_text"].isin(excluded_texts)
        & ~df["aa_text"].str.isupper()
    ]

    texto_lista = df_filtered["aa_text"].to_numpy()
    df_partidos = extraer_partidos(texto_lista)
    end_time = time.time()
    execution_time = end_time - start_time



    print(df_partidos)
    print(
        f"Tiempo de ejecución: {execution_time:.2f} segundos\n"
    )
