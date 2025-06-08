import json
import asyncio
import websockets
from common import queue_in

from common import queue_in, clientes_emuladores

async def receiver_handler(websocket):
    mensaje_inicio = await websocket.recv()
    data = json.loads(mensaje_inicio)

    if data.get("rol") != "emulador":
        await websocket.close()
        return

    print("🟣 Emulador conectado.")
    clientes_emuladores.add(websocket)

    try:
        async for mensaje in websocket:
            await queue_in.put(mensaje)
            print("📥 Mensaje recibido y encolado.")
    except websockets.exceptions.ConnectionClosed:
        print("🔌 Emulador desconectado.")
    finally:
        clientes_emuladores.discard(websocket)

async def start_receiver(host="0.0.0.0", port=8765):
    print(f"🎧 Esperando emuladores en ws://{host}:{port}")
    return await websockets.serve(receiver_handler, host, port)
