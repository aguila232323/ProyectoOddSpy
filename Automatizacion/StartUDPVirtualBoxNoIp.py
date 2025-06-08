import socket
import subprocess
import threading

# Configuraciones
UDP_IP = "0.0.0.0"
UDP_PORT = 9999  # Usamos un solo puerto para todos los comandos


def ejecutar_adb_conectar(ip_dispositivo):
    """Intenta conectar con el dispositivo via ADB"""
    comando_connect = f"adb connect {ip_dispositivo}"
    resultado_connect = subprocess.run(
        comando_connect,
        shell=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    print(
        f"🔌 Resultado de ADB Connect ({ip_dispositivo}):\n{resultado_connect.stdout}"
    )
    if resultado_connect.stderr:
        print(f"🔴 Errores en ADB Connect:\n{resultado_connect.stderr}")
    return "connected" in resultado_connect.stdout.lower()


def ejecutar_comando_dispositivo(ip_dispositivo, comando_script):
    """Ejecuta un comando específico en el dispositivo conectado"""
    comando = [
        "adb",
        "-s",
        ip_dispositivo,
        "shell",
        "su",
        "-c",
        f"/data/data/com.termux/files/usr/bin/python3 {comando_script}",
    ]

    resultado = subprocess.run(
        " ".join(comando),
        shell=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )

    print(f"🔷 Salida del comando {comando_script}:")
    print(resultado.stdout)
    if resultado.stderr:
        print("🔴 Errores:")
        print(resultado.stderr)


def reiniciar_dispositivo(ip_dispositivo):
    """Reinicia el dispositivo usando adb reboot"""
    comando = ["adb", "-s", ip_dispositivo, "reboot"]

    resultado = subprocess.run(
        comando,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )

    print(f"🟡 Salida del comando reboot ({ip_dispositivo}):")
    print(resultado.stdout)
    if resultado.stderr:
        print("🔴 Errores:")
        print(resultado.stderr)


def manejar_dispositivo(ip_origen, accion):
    """Maneja las diferentes acciones para un dispositivo"""
    ip_dispositivo = f"{ip_origen}:5555"

    print(f"🟢 Nueva solicitud de {accion} desde {ip_origen}")

    if not ejecutar_adb_conectar(ip_dispositivo):
        print(f"❌ Falló la conexión ADB con {ip_dispositivo}")
        return

    if accion == "activar":
        print(f"✅ Ejecutando script de activación en {ip_dispositivo}")
        ejecutar_comando_dispositivo(
            ip_dispositivo, "/sdcard/Webscraping/StartScript.py"
        )
    elif accion == "reanudar":
        print(f"🔄 Ejecutando script de reanudación en {ip_dispositivo}")
        ejecutar_comando_dispositivo(
            ip_dispositivo, "/sdcard/Webscraping/GenericStartSettings.py"
        )
    elif accion == "reiniciar":
        print(f"🔄 Reiniciando dispositivo {ip_dispositivo}")
        reiniciar_dispositivo(ip_dispositivo)
    else:
        print(f"❌ Acción desconocida: {accion}")


def iniciar_servidor_udp():
    """Inicia el servidor UDP que escucha comandos"""
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((UDP_IP, UDP_PORT))

    print(f"🚀 Servidor UDP iniciado en puerto {UDP_PORT}")
    print("Escuchando comandos: 'activar', 'reanudar', 'reiniciar'")

    while True:
        data, addr = sock.recvfrom(1024)
        mensaje = data.decode(errors="ignore").strip().lower()
        ip_origen = addr[0]

        print(f"📨 Mensaje recibido de {ip_origen}: {mensaje}")

        if mensaje in ["activar", "reanudar", "reiniciar"]:
            # Creamos un hilo para manejar cada dispositivo
            threading.Thread(
                target=manejar_dispositivo, args=(ip_origen, mensaje), daemon=True
            ).start()
        else:
            print(f"⚠️ Comando no reconocido: {mensaje}")


if __name__ == "__main__":
    # Iniciamos el servidor en el hilo principal
    iniciar_servidor_udp()
