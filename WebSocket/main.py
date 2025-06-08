import asyncio
from Reciver import start_receiver
from Sender import send_to_receptors
from receiver_handler import start_receptor_listener

async def main():
    await start_receiver()
    await start_receptor_listener()
    await send_to_receptors()

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("🛑 Servidor detenido manualmente.")
