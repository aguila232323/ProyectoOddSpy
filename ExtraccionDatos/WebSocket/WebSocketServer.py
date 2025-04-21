import asyncio
import websockets
import json

RUTA_ARCHIVO = "C:\\Users\\Prados\\Desktop\\datos.json"


async def receive_data(websocket, path=None):
    async for message in websocket:
        try:
            data = json.loads(message)

            with open(RUTA_ARCHIVO, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=4)

            print(f"✅ Datos guardados en: {RUTA_ARCHIVO}")

        except json.JSONDecodeError:
            print("❌ Error: Mensaje no es un JSON válido")


async def main():
    async with websockets.serve(receive_data, "0.0.0.0", 8765):
        print("🚀 Servidor WebSocket iniciado en ws://localhost:8765")
        await asyncio.Future()


if __name__ == "__main__":
    asyncio.run(main())
