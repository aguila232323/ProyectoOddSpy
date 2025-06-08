import subprocess
import time
import sys
import os


def run_python_script(script_path):
    """Ejecuta un script Python y maneja posibles errores"""
    if not os.path.exists(script_path):
        print(f"Error: El archivo {script_path} no existe")
        return False

    try:
        print(f"\nEjecutando {script_path}...")
        result = subprocess.run(
            [sys.executable, script_path], check=True, text=True, capture_output=True
        )
        print(f"Salida:\n{result.stdout}")
        return True
    except subprocess.CalledProcessError as e:
        print(f"Error al ejecutar {script_path}:\n{e.stderr}")
        return False


if __name__ == "__main__":
    scripts = [
        "/sdcard/Webscraping/GenericStartSettingsInterwetten.py",
        "/sdcard/Webscraping/InterwettenScript.py",
    ]

    delay_seconds = 5  # Tiempo de espera entre scripts

    for script in scripts:
        success = run_python_script(script)

        if not success:
            print(f"Deteniendo la ejecución debido a un error en {script}")
            sys.exit(1)

        if script != scripts[-1]:  # Si no es el último script
            print(f"\nEsperando {delay_seconds} segundos...")
            time.sleep(delay_seconds)

    print("\nTodos los scripts se ejecutaron correctamente")
