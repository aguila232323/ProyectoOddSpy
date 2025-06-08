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
# DesktopView : True


def es_numero_decimal(texto):
    return bool(re.fullmatch(r"\d+\.\d+", str(texto)))


def es_numero_coma(texto):
    return bool(re.fullmatch(r"\d+\,\d+", str(texto)))


def es_hora_o_fecha(texto):
    return bool(
        re.fullmatch(r"\d{1,2}:\d{2}", texto)
        or re.fullmatch(r"Comienza en \d{1,3}′?", texto)
        or re.fullmatch(r"Comienza en \d{1,3} (min|′)", texto)
        or re.fullmatch(r"\d{1,2}/\d{1,2}(/\d{4})?", texto)
        or re.fullmatch(r"[A-Za-z]{3} \d{1,2} [A-Za-z]{3} \d{1,2}:\d{2}", texto)
        or re.fullmatch(r"\s*(Mañana|Hoy)\s*\d{1,2}:\d{2}\s*", texto)
        or re.fullmatch(r"\s*(Mañana|Hoy)\s*/\s*\d{1,2}:\d{2}\s*", texto)
        or re.fullmatch(r"en\s\d+\smin", texto)
    )


def es_nombre_valido(texto):
    return not re.fullmatch(r"\d+(:\d+)?", texto)


def extraer_partidos(arr):
    partidos = []
    for i in range(len(arr) - 5):
        if (
            isinstance(arr[i], str)  # Fecha (21/03)
            and isinstance(arr[i + 1], str)  # Hora (14:00)
            and isinstance(
                arr[i + 2], str
            )  # Equipos (HAPOEL TEL AVIV VS. HAPOEL AFULA)
            and "VS." in arr[i + 2]  # Validar que los equipos estén separados por VS.
            and es_numero_coma(arr[i + 3])  # Validar cuota A
            and es_numero_coma(arr[i + 4])  # Validar cuota empate
            and es_numero_coma(arr[i + 5])  # Validar cuota B
        ):
            # Unir fecha y hora en una sola columna
            if ','  not in arr[i]:
                fecha_hora = f"{arr[i]} {arr[i + 1]}"
            else:
                fecha_hora = f"{arr[i + 1]}"


            # Separar equipos por el delimitador "VS."
            equipos = arr[i + 2].split("VS.")
            if len(equipos) == 2:
                equipo_a = equipos[0].strip()
                equipo_b = equipos[1].strip()
            else:
                equipo_a = arr[i + 2]
                equipo_b = ""

            partidos.append(
                [
                    equipo_a,  # Equipo A
                    equipo_b,  # Equipo B
                    fecha_hora,
                    arr[i + 3],  # Cuota A
                    arr[i + 4],  # Cuota empate
                    arr[i + 5],  # Cuota B
                    "FootBall",
                    "ApuestasAndalucia",
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

        #Obtengo elementos de la app

        df = te.get_df_uiautomator2(with_screenshot=False)

        #Filtros
        cond_name = df["aa_text"].str.contains("FC", na=False)
        cond_fechas_horas = df["aa_text"].apply(es_hora_o_fecha)
        cond_numeros_comas = df["aa_text"].apply(es_numero_coma)

        #Aplico los filtros y cogo la moda
        aa_indent_values_Name = (df.loc[cond_name, "aa_indent"].mode().iloc[0] if cond_name.any() else None)
        aa_indent_values_FechasHoras = (df.loc[cond_fechas_horas, "aa_indent"].mode().iloc[0]if cond_fechas_horas.any()else None)
        aa_indent_values_NumerosComas = (df.loc[cond_numeros_comas, "aa_indent"].mode().iloc[0]if cond_numeros_comas.any()else None)
        aa_indent_values = {aa_indent_values_Name,aa_indent_values_FechasHoras,aa_indent_values_NumerosComas,} - {None}

        #Filtro a partir de donde comienzan los partidos

        indice_primer_en_juego = df[df["aa_text"].str.contains("HOY", na=False)].index.min()
        df_filtrado = df.loc[indice_primer_en_juego:]

        #Filtro el resto de elementos

        excluded_texts = {
            " ",
            "X",
            "1",
            "2",
            "+",
            "AHORA EN LIVE",
            "1X2",
            "AMBOS GOL",
            "&#10;",
            "Nº GOLES",
            "Nº GOLES (2,5)",
            "&gt;",
        }
        allowed_clazz = {"android.view.View"}

        df_filtered = df[
            df["aa_indent"].isin(aa_indent_values)
            & df["aa_clazz"].isin(allowed_clazz)
            & ~df["aa_text"].isin(excluded_texts)
            & ~df["aa_text"].str.contains(r"^\d+$", na=False)
            & ~(
                (df["aa_indent"] == aa_indent_values_FechasHoras)
                & df["aa_text"].str.match(r"^[12X]\s\d+,\d{2}$", na=False)
            )
            & ~df["aa_text"].str.contains(r"\(\+\s\d+\)", na=False)
            & ~df["aa_text"].str.contains(r"[a-z]", regex=True, na=False)
            | df["aa_text"].apply(es_hora_o_fecha) & df["aa_text"].str.strip().ne("")
        ]

        texto_lista = df_filtered["aa_text"].to_numpy()
        df_partidos = extraer_partidos(texto_lista)
        end_time = time.time()
        execution_time = end_time - start_time

        #Envia los datos
        await emulador.enviar_datos(df_partidos)
        print(f"Tiempo de ejecución: {execution_time:.2f} segundos\n")

if __name__ == "__main__":
    asyncio.run(main())