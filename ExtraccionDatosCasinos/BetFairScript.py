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
        except (websockets.exceptions.ConnectionClosedError,
                websockets.exceptions.ConnectionClosedOK,
                websockets.exceptions.WebSocketException) as e:
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

# Navegador : Browser
#DesktopView : True

def es_numero_decimal(texto):
    return bool(re.fullmatch(r"\d+\.\d+", str(texto)))

def enviar_udp(mensaje, ip, puerto):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.sendto(mensaje.encode(), (ip, puerto))
    sock.close()

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
                    "FootBall",
                    "Betfair",
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
    # Creamos el objeto emulador
    emulador = EmuladorWebSocket("ws://192.168.1.146:8765")

    # Conectar al WebSocket
    await emulador.conectar()

    while True:
        start_time = time.time()

        # Obtener elementos de la app
        df = te.get_df_uiautomator2(with_screenshot=False)

        # Aplicar los filtros
        cond_name = df["aa_text"].str.contains("FC", na=False)
        cond_decimals = df["aa_text"].str.contains(r"\d+\.\d+", na=False)
        cond_fechas_horas = df["aa_text"].apply(es_hora_o_fecha)

        aa_indent_values_Name = (
            df.loc[cond_name, "aa_indent"].mode().iloc[0] if cond_name.any() else None
        )
        aa_indent_values_Decimals = (
            df.loc[cond_decimals, "aa_indent"].mode().iloc[0]
            if cond_decimals.any()
            else None
        )
        aa_indent_values_FechasHoras = (
            df.loc[cond_fechas_horas, "aa_indent"].mode().iloc[0]
            if cond_fechas_horas.any()
            else None
        )
        aa_indent_values = {
            aa_indent_values_Name,
            aa_indent_values_Decimals,
            aa_indent_values_FechasHoras,
        } - {None}

        # Filtro a partir de donde comienzan los partidos
        indice_primer_en_juego = df[
            df["aa_text"].str.contains("Más/menos de 2,5 goles", na=False)
        ].index.min()
        df_filtrado = df.loc[indice_primer_en_juego:]

        # Filtro el resto de los elementos
        excluded_texts = {
            "",
            "-",
            "SUSPENDIDO",
            "CuotasTOP",
            "Ver en directo",
            "DAZN",
            "Ganancias a 90 minutos",
            "Todos los mercados",
            "La\xa0Liga\xa0TV\xa0Hypermotion",
            "\u2032",
            "Movistar LaLiga TV",
            "Irá a En Juego",
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



    #Comprobamos que no haya chasheado Firefox
        if df_partidos.empty and df['aa_text'].str.contains('bwin', case=False, na=False).any() and df['aa_text'].str.contains('Interwetten', case=False, na=False).any():
            enviar_udp("reiniciar", "192.168.1.146", 9999)
            print("reiniciando....")
            time.sleep(1000)


        if not df_partidos.empty:
            await emulador.enviar_datos(df_partidos)
            print(df_partidos)



if __name__ == "__main__":
    asyncio.run(main())