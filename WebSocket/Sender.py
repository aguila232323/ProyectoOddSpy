import asyncio
import json
from collections import Counter
from datetime import datetime
from Websocket.ProcesadorApuestas import ProcesadorApuestas
from common import queue_in, clientes_receptores
from common import clientes_emuladores
INTERVALO_ENVIO = 5  # segundos entre envios
ARBITRAJE_SUREBET = 1.07
ARBITRAJE_COMPARACIONES = 0.0
IMAGEN_POR_DEFECTO = "https://png.pngtree.com/png-vector/20230419/ourmid/pngtree-white-shield-vector-png-image_6714259.png"
BENEFICIO_LIMITE = -7
NUM_PARTIDOS = []

procesador = ProcesadorApuestas("equipos_estandar.json")
contador = Counter(NUM_PARTIDOS)
# Almacenamiento temporal
partidos_almacenados = {
    "surebets": set(),
    "comparaciones": set(),
    "ofertas": set()
}


def formatear_partido_por_clave(clave, procesador):
    datos = procesador.datos_por_partido.get(clave)
    if not datos:
        print(f"⚠️ Clave no encontrada: {clave}")
        return None

    surebet_info = procesador.detectar_surebets(clave) or {}
    arbitraje = float(surebet_info.get("arbitraje", 1))
    beneficio = round((1 - arbitraje) * 100, 2)

    tipo = (
        "Surebet 💰" if beneficio > 0 else
        "BonusLiberator 🎁" if beneficio >= BENEFICIO_LIMITE else
        "Comparer 📊"
    )

    mejores = datos.get("Mejores_Cuotas", {})

    def obtener_cuota(mercado):
        mercado_data = mejores.get(mercado, {})
        return {
            f"Casino{mercado}": mercado_data.get("casino", ""),
            f"Odds{mercado}": str(mercado_data.get("cuota", ""))
        }

    return {
        "HomeTeam": datos.get("HomeTeam", ""),
        "HomeTeamImg": datos.get("HomeTeamImg", "") or IMAGEN_POR_DEFECTO,
        "AwayTeam": datos.get("AwayTeam", ""),
        "AwayTeamImg": datos.get("AwayTeamImg", "") or IMAGEN_POR_DEFECTO,
        "Time": datos.get("Time", ""),
        **obtener_cuota("1"),
        **obtener_cuota("X"),
        **obtener_cuota("2"),
        "Type": tipo,
        "BenefitPecentaje": f"{beneficio}%",
        "LastUpdate": datetime.now().isoformat()
    }


async def procesar_mensaje(mensaje):
    try:
        datos_recibidos = json.loads(mensaje)
        if not isinstance(datos_recibidos, list):
            datos_recibidos = [datos_recibidos]

        nuevas_claves = set()
        for partido in datos_recibidos:
            if not all(key in partido for key in ["HomeTeam", "AwayTeam"]):
                print(f"❌ Partido inválido: {partido}")
                continue

            home = procesador.estandarizar_nombre(partido['HomeTeam'])
            away = procesador.estandarizar_nombre(partido['AwayTeam'])
            clave = f"{home} vs {away}"

            procesador.actualizar_partido(partido)
            nuevas_claves.add(clave)

        return nuevas_claves
    except json.JSONDecodeError:
        print("❌ Error: Mensaje JSON inválido")
    except Exception as e:
        print(f"⚠️ Error inesperado al procesar mensaje: {e}")
    return set()


async def clasificar_partidos(claves):
    for clave in claves:
        surebet = procesador.detectar_surebets(clave)
        if not surebet:
            continue

        arbitraje = surebet.get("arbitraje", 1)

        if arbitraje <= ARBITRAJE_SUREBET:
            partidos_almacenados["surebets"].add(clave)
        if arbitraje > ARBITRAJE_COMPARACIONES:
            partidos_almacenados["comparaciones"].add(clave)

        partidos_almacenados["ofertas"].add(clave)


async def enviar_lotes():
    while True:
        await asyncio.sleep(INTERVALO_ENVIO)

        for tipo, claves in partidos_almacenados.items():
            if not claves or not clientes_receptores[tipo]:
                continue

            mensaje_filtrado = []
            for clave in claves:
                partido = formatear_partido_por_clave(clave, procesador)
                if partido:
                    mensaje_filtrado.append(partido)

            if mensaje_filtrado:
                mensaje_json = json.dumps(mensaje_filtrado)
                await enviar_a_receptores(tipo, clientes_receptores[tipo], mensaje_json)


        #if len(clientes_emuladores) > 1 & elemento_mas_repetido != len(json.loads(mensaje):
        if len(clientes_emuladores) > 1 and len(clientes_receptores)>0:
            for clave in partidos_almacenados:
                partidos_almacenados[clave].clear()
            print("🔄")



async def enviar_a_receptores(tipo, receptores, mensaje):
    receptores_activos = []
    for receptor in list(receptores):
        try:
            await receptor.send(mensaje)
            receptores_activos.append(receptor)
            NUM_PARTIDOS.append(json.loads(mensaje))
            print(len(clientes_emuladores))

            print(f"✓ Enviados {len(json.loads(mensaje))} partidos a {tipo}")
        except Exception as e:
            print(f"✗ Error enviando a {tipo}: {str(e)}")

    # Actualizar a los receptores
    clientes_receptores[tipo] = set(receptores_activos)


async def send_to_receptors():
    # Inicia tarea de envio periodico
    tarea_envio = asyncio.create_task(enviar_lotes())

    try:
        while True:
            mensaje = await queue_in.get()
            nuevas_claves = await procesar_mensaje(mensaje)
            if nuevas_claves:
                await clasificar_partidos(nuevas_claves)
    finally:
        tarea_envio.cancel()
        try:
            await tarea_envio
        except asyncio.CancelledError:
            pass