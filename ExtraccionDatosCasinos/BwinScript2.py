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
import subprocess
import socket
class EmuladorWebSocket:
    def __init__(self, uri):
        self.uri = uri
        self.websocket = None

    async def conectar(self):
        """Establecer conexión solo una vez al inicio"""
        try:
            self.websocket = await websockets.connect(self.uri)
            await self.websocket.send(json.dumps({"rol": "emulador"}))
            print("🆔 Emulador Conectado.")
        except Exception as e:
            print(f"❌ Error al conectar: {e}")
            self.websocket = None

    async def enviar_datos(self, df_partidos):
        if self.websocket is None:
            await self.conectar()
            if self.websocket is None:
                print("❌ No se pudo conectar.")
                return
        try:
            json_partidos = df_partidos.to_json(orient="records")
            await self.websocket.send(json_partidos)
            print("📤 JSON enviado.")
        except (
            websockets.exceptions.ConnectionClosedError,
            websockets.exceptions.ConnectionClosedOK,
            websockets.exceptions.WebSocketException,
        ) as e:
            print(f"⚠️ Conexión WebSocket rota: {e}")
            self.websocket = None
        except Exception as e:
            print(f"⚠️ Error al enviar datos: {e}")
            self.websocket = None


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
#DesktopView : False

# Funciones auxiliares
def es_numero_decimal(texto):
    return bool(re.fullmatch(r"\d+\.\d+", str(texto)))

def enviar_udp(mensaje, ip, puerto):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.sendto(mensaje.encode(), (ip, puerto))
    sock.close()
def es_numero_coma(texto):
    return bool(re.fullmatch(r"\d+\,\d+", str(texto)))


def es_hora_o_fecha(texto):
    return bool(
        re.fullmatch(r"\d{1,2}:\d{2}", texto)
        or re.fullmatch(r"Comienza en \d{1,3}′?", texto)
        or re.fullmatch(r"\d{1,2}/\d{1,2}/\d{4}", texto)
        or re.fullmatch(r"[A-Za-z]{3} \d{1,2} [A-Za-z]{3} \d{1,2}:\d{2}", texto)
        or re.fullmatch(r"(Mañana|Hoy) \d{1,2}:\d{2}", texto)
        or re.fullmatch(r"(Mañana|Hoy) / \d{1,2}:\d{2}", texto)
        or re.fullmatch(r"\d{1,2}\.\d{2}\. - \d{1,2}:\d{2}", texto)
        or re.fullmatch(r"\d{1,3} min", texto)
        or re.fullmatch(r"^Live$", texto)
        or re.fullmatch(r"^ahora$", texto)
    )


def es_nombre_valido(texto):
    return not (
        re.fullmatch(r"\d+(:\d+)?", texto)
        or es_hora_o_fecha(texto)
        or es_numero_decimal(texto)
    )

def extraer_partidos(arr):
    partidos = []
    for i in range(len(arr) - 6):
        if (
            isinstance(arr[i], str)
            and isinstance(arr[i], str)
            and es_nombre_valido(arr[i])  # Validar nombre equipo A
            and isinstance(arr[i + 1], str)
            and es_nombre_valido(arr[i + 1])  # Validar nombre equipo B
            and es_hora_o_fecha(arr[i + 2])  # Fecha
            and es_numero_decimal(arr[i + 3])  # Cuota A
            and es_numero_decimal(arr[i + 4])  # Cuota Empate
            and es_numero_decimal(arr[i + 5])  # Cuota B
        ):
            partidos.append(
                [
                    arr[i],  # Equipo A
                    arr[i + 1],  # Equipo B
                    arr[i + 2],  # Fecha
                    arr[i + 3],  # Cuota A
                    arr[i + 4],  # Cuota Empate
                    arr[i + 5],  # Cuota B
                    "FootBall",
                    "Bwin",
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
            "Sport",
            "Casino",
        ],
    )

async def main():
    emulador = EmuladorWebSocket("ws://192.168.1.146:8765")

    # Conectar al WebSocket
    await emulador.conectar()


    myshell = te.open_shell(
    buffer_size=40960,
    exit_command=b"exit",
    print_stdout=False,
    print_stderr=False,
    )



    while True:
        start_time = time.time()

        #Obtiene los elementos de la app

        df = te.get_df_uiautomator2(with_screenshot=False)

        #Filtros
        cond_name = df["aa_text"].str.contains(r"(FC|SC|AFC)", na=False)
        cond_decimals = df["aa_text"].str.contains(r"\d+\.\d+", na=False)
        cond_fechas_horas = df["aa_text"].apply(es_hora_o_fecha)
        cond_numeros_comas = df["aa_text"].apply(es_numero_coma)

        #Aplico los filtros y cogo la moda
        aa_indent_values_Name = df.loc[cond_name, "aa_indent"].mode().iloc[0] if cond_name.any() else None
        aa_indent_values_Decimals = df.loc[cond_decimals, "aa_indent"].mode().iloc[0] if cond_decimals.any() else None
        aa_indent_values_FechasHoras = df.loc[cond_fechas_horas, "aa_indent"].mode().iloc[0] if cond_fechas_horas.any() else None
        aa_indent_values_NumerosGoles = df.loc[cond_numeros_comas, "aa_indent"].mode().iloc[0] if cond_numeros_comas.any() else None
        aa_indent_values = {aa_indent_values_Name, aa_indent_values_Decimals, aa_indent_values_FechasHoras, aa_indent_values_NumerosGoles} - {None}


        if aa_indent_values_FechasHoras is not None:
            df.loc[(df["aa_indent"] == aa_indent_values_FechasHoras) & df["aa_text"].str.isdigit(),"aa_text",] = "Live"

        #Filtro resto de elementos

        excluded_texts = {
            "",
            "Comienza en ",
            "Sub-20",
            "Sub-21",
            "Sub-22",
            "Sub-23",
            "Sub-20/F",
            "Reservas",
            "Apostar ahora",
            "Juvenil"
            "X",
            "1",
            "2",
            "\uea8c",
            "F",
            "CREA TU APUESTA",
        }

        allowed_indexs = {1, 4, 0, 2}

        df_filtered = df[
            df["aa_index"].isin(allowed_indexs)
            & df["aa_indent"].isin(aa_indent_values)
            & (df["aa_clazz"] == "android.view.View")
            & ~df["aa_text"].isin(excluded_texts)
            & ~df["aa_text"].str.isupper()
        ]

        mask = (df_filtered["aa_text"] == "Live") & (df_filtered["aa_text"].shift() == "Live")
        df_sinLive = df_filtered[~mask].copy()
        texto_lista = df_sinLive["aa_text"].to_numpy()
        df_partidos = extraer_partidos(texto_lista)


        #Asegurarnos de que esta siempre al final de la pagina

        df2 = te.get_df_uiautomator2(with_screenshot=False)
        youtube_rows = df2[df2["aa_text"] == "youtube"]
        if not youtube_rows.empty:
            youtube_row = youtube_rows.iloc[0]
            if youtube_row["aa_visible_to_user"] == 0:
                duration_ms = 30
                while youtube_row["aa_visible_to_user"] == 0:
                    myshell.sh_input_swipe(770, 726, 770, 292, duration_ms, timeout=10)
                    df2 = te.get_df_uiautomator2(with_screenshot=False)
                    youtube_rows = df2[df2["aa_text"] == "youtube"]
                    if youtube_rows.empty:
                        break
                    youtube_row = youtube_rows.iloc[0]

        #Comprobamos que no haya chasheado Firefox
        if df_partidos.empty and df['aa_text'].str.contains('bwin', case=False, na=False).any() and df['aa_text'].str.contains('Interwetten', case=False, na=False).any():
            enviar_udp("reiniciar", "192.168.1.146", 9999)
            time.sleep(1000)


        if not df_partidos.empty:
            await emulador.enviar_datos(df_partidos)
            print(df_partidos)


if __name__ == "__main__":
    asyncio.run(main())

