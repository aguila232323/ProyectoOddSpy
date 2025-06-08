import asyncio

#Almacena mensajes entrantes de emuladores
queue_in = asyncio.Queue()

#Almacena los receptores conectados separados por tipo
clientes_receptores = {
    "surebets": set(),
    "comparaciones": set(),
    "ofertas": set()
}

# Almacenar conexiones websocket de emuladores
clientes_emuladores = set()



