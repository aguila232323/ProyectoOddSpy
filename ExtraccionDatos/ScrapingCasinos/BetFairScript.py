from cyandroemu import TermuxAutomation, clean_zombies
import pandas as pd
from time import sleep as timesleep
import atexit
import numpy as np
import re
import time
import asyncio
import websockets
import json
import time


async def send_data(df_partidos):
    uri = "ws://192.168.1.75:8765"
    async with websockets.connect(uri) as websocket:
        json_partidos = df_partidos.to_json(
            orient="records"
        )  # Convertir DataFrame a JSON
        await websocket.send(json_partidos)
        print("📤 JSON enviado al servidor:", json_partidos)





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

# Navegador : Browser
#DesktopView : True

def es_numero_decimal(texto):
    return bool(re.fullmatch(r"\d+\.\d+", str(texto)))


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
    for i in range(len(arr) - 7):
        if (
            isinstance(arr[i], str)
            and es_hora_o_fecha(arr[i])  # Fecha
            and es_numero_decimal(arr[i + 1])  # Cuota más de 2.5
            and es_numero_decimal(arr[i + 2])  # Cuota menos de 2.5
            and es_numero_decimal(arr[i + 3])  # Cuota Equipo A
            and es_numero_decimal(arr[i + 4])  # Cuota Empate
            and es_numero_decimal(arr[i + 5])  # Cuota Equipo B
            and isinstance(arr[i + 6], str)
            and es_nombre_valido(arr[i + 6])  # Validar nombre equipo A
            and isinstance(arr[i + 7], str)
            and es_nombre_valido(arr[i + 7])  # Validar nombre equipo B
        ):
            partidos.append(
                [
                    arr[i + 6],  # Equipo A
                    arr[i + 7],  # Equipo B
                    arr[i],  # Fecha
                    arr[i + 3],  # Cuota A
                    arr[i + 4],  # Cuota Empate
                    arr[i + 5],  # Cuota B
                    arr[i + 1],  # Cuota Más 2.5
                    arr[i + 2],  # Cuota Menos 2.5
                ]
            )

    return pd.DataFrame(
        partidos,
        columns=[
            "HomeTeam",
            "AwayTeam",
            "Time",
            "Odds1",
            "OddsX",
            "Odds2",
            "Over25",
            "Under25",
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

    #APLICAMOS LOS FILTROS Y COGO LA MODA
    aa_indent_values_Name = df.loc[cond_name, "aa_indent"].mode().iloc[0] if cond_name.any() else None
    aa_indent_values_Decimals = df.loc[cond_decimals, "aa_indent"].mode().iloc[0] if cond_decimals.any() else None
    aa_indent_values_FechasHoras = df.loc[cond_fechas_horas, "aa_indent"].mode().iloc[0] if cond_fechas_horas.any() else None
    aa_indent_values = {aa_indent_values_Name, aa_indent_values_Decimals, aa_indent_values_FechasHoras} - {None}

    # FILTRO A PARTIR DE DONDE COMIENZAN LOS PARTIDOS

    indice_primer_en_juego = df[
        df["aa_text"].str.contains("Más/menos de 2,5 goles", na=False)
    ].index.min()
    df_filtrado = df.loc[indice_primer_en_juego:]


    #FILTRO RESTO ELEMENTOS

    excluded_texts = {
        "", "-", "SUSPENDIDO", "CuotasTOP", "Ver en directo",
        "Ganancias a 90 minutos", "Todos los mercados"
    }
    allowed_clazz = {"android.view.View", "android.widget.TextView"}

    df_filtered = df_filtrado[
        (df_filtrado["aa_indent"].isin(aa_indent_values))
        & (df_filtrado["aa_clazz"].isin(allowed_clazz))
        & ~(
            (df_filtrado["aa_indent"] == aa_indent_values_Name)
            & (df_filtrado["aa_text"].apply(es_hora_o_fecha))
        )
        & (~df_filtrado["aa_text"].isin(excluded_texts))
        & (~df_filtrado["aa_text"].str.contains(";", na=False))
    ]

    texto_lista = df_filtered["aa_text"].to_numpy()
    df_partidos = extraer_partidos(texto_lista)
    end_time = time.time()
    execution_time = end_time - start_time



    asyncio.run(send_data(df_partidos))
    print(
        f"Tiempo de ejecución: {execution_time:.2f} segundos\n"
    )
