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
        or re.fullmatch(r"(Mañana|Hoy) \d{1,2}:\d{2}", texto)  # "Hoy 14:00"
        or re.fullmatch(r"(Mañana|Hoy) / \d{1,2}:\d{2}", texto)  # "Hoy / 18:30"
        or re.fullmatch(r"\d{1,2}\.\d{2}\. - \d{1,2}:\d{2}", texto)  # "28.04. - 14:00"
    )


def es_nombre_valido(texto):
    return not re.fullmatch(r"\d+(:\d+)?", texto)


def extraer_partidos(arr):
    partidos = []
    for i in range(len(arr) - 5):
        if (
            isinstance(arr[i], str)
            and es_nombre_valido(arr[i])  # EquipoA
            and isinstance(arr[i + 1], str)
            and es_nombre_valido(arr[i + 1])  # EquipoB
            and isinstance(arr[i + 2], str)
            and es_hora_o_fecha(arr[i + 2])  # Horario
            and es_numero_decimal(arr[i + 3])  # CuotaA
            and es_numero_decimal(arr[i + 4])  # Empate
            and es_numero_decimal(arr[i + 5])  # CuotaB
        ):
            partidos.append(
                [
                    arr[i],  # EquipoA
                    arr[i + 1],  # EquipoB
                    arr[i + 2],  # Horario
                    arr[i + 3],  # CuotaA
                    arr[i + 4],  # Empate
                    arr[i + 5],  # CuotaB
                    "FootBall",
                    "Interwetten",
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

    emulador = EmuladorWebSocket("ws://192.168.1.75:8765")

    # Conectar al WebSocket
    await emulador.conectar()

    while True:

        start_time = time.time()

        #Obtiene elementos de la app

        df = te.get_df_uiautomator2(with_screenshot=False)

        allowed_indexs = {0}
        cond_name = (df["aa_index"].isin(allowed_indexs)) & (df["aa_text"].str.contains("FC", na=False))


        #aplico el filtro y cogo la moda
        aa_indent_values_Name = df.loc[cond_name, "aa_indent"].mode().iloc[0] if cond_name.any() else None
        aa_indent_values = {aa_indent_values_Name} - {None}


        #Filtro a partir de donde comienzan los partidos

        index_primer_elemento = df[df["aa_text"].str.startswith("Ap.")].index[0]
        df_filtrado = df.loc[index_primer_elemento:]


        #Filtro los demas datos

        excluded_texts = {"X",
                        "\ue914",
                        "\ue943",
                        ""}
        allowed_clazz = {"android.view.View", "android.widget.TextView"}


        df_filtered = df_filtrado[
            df_filtrado["aa_index"].isin(allowed_indexs)
            & df_filtrado["aa_indent"].isin(aa_indent_values)
            & (df_filtrado["aa_clazz"].isin(allowed_clazz))
            & ~df_filtrado["aa_text"].isin(excluded_texts)
            & (~df_filtrado["aa_text"].str.match(r"^\d+$", na=False))
            & (~df_filtrado["aa_text"].str.contains(r"\bISO|ID\b", na=False))
        ]

        texto_lista = df_filtered["aa_text"].to_numpy()
        df_partidos = extraer_partidos(texto_lista)
        end_time = time.time()
        execution_time = end_time - start_time
        await emulador.enviar_datos(df_partidos)
        print(
            f"Tiempo de ejecución: {execution_time:.2f} segundos\n"
        )

        #Refresco la pagina para actualizar la informacion
        df = pd.DataFrame()
        while df.empty:
            df = te.get_df_uiautomator2(with_screenshot=False)
            df = df.loc[(df.aa_content_desc == "Refresh")]
        df.aa_input_tap.iloc[0]()
        timesleep(2)


if __name__ == "__main__":
    asyncio.run(main())