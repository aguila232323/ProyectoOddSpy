
import json
import websockets
from common import clientes_receptores

async def receptor_handler(websocket):
    mensaje_inicio = await websocket.recv()
    data = json.loads(mensaje_inicio)

    if data.get("rol") != "receptor":
        await websocket.close()
        return

    tipo = data.get("tipo")
    if tipo not in clientes_receptores:
        print(f"❌ Tipo de receptor inválido: {tipo}")
        await websocket.close()
        return

    print(f"🟢 Receptor conectado para tipo: {tipo}")
    clientes_receptores[tipo].add(websocket)

    try:
        await websocket.wait_closed()
    finally:
        clientes_receptores[tipo].discard(websocket)
        print(f"🔌 Receptor desconectado de tipo: {tipo}")

async def start_receptor_listener(host="0.0.0.0", port=8766):
    print(f"📲 Esperando receptores en ws://{host}:{port}")
    return await websockets.serve(receptor_handler, host, port)
