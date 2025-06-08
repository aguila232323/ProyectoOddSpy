from rapidfuzz import process, fuzz
import json
import regex as re
import time
class ProcesadorApuestas:
    def __init__(self, ruta_json_equipos):
        self.equipos_estandar = self._cargar_equipos(ruta_json_equipos)
        self.cache_estandar = {}
        self.datos_por_partido = {}
        self.nombres_no_encontrados = set()

    def _cargar_equipos(self, ruta):
        with open(ruta, 'r', encoding='utf-8') as f:
            data = json.load(f)
            clubes = data["ClubData"]
            return {club["Name"]: club for club in clubes}

    def _cargar_equipos_completo(self):
        with open("equipos_estandar.json", "r", encoding="utf-8") as f:
            return json.load(f)["ClubData"]

    def _obtener_imagen_equipo(self, nombre_estandar):
        for club in self._cargar_equipos_completo():
            if club["Name"] == nombre_estandar:
                return club.get("ImageURL", "")
        return ""


    def guardar_nombres_no_encontrados(self):
        with open("nombres_no_encontrados.json", "w", encoding="utf-8") as f:
            json.dump(list(self.nombres_no_encontrados), f, ensure_ascii=False, indent=2)

    def limpiar_nombre(self, nombre):
        nombre = nombre.lower()
        nombre = re.sub(r'\b(utd|u-19|u-23|ii|ca|sp|sportivo|(u20)|u20|sub-20|(w)|(f)|sub-21|jrs|sk|ao|(res)|united|fc|fk|club|if|deportivo|cf|sc|el|la)\b', '', nombre)  # elimina palabras comunes
        nombre = re.sub(r'[^a-zA-Z0-9 ]', '', nombre)  # elimina puntuación
        return nombre.strip()

    def estandarizar_nombre(self, nombre):
        if nombre not in self.cache_estandar:
            nombre_limpio = self.limpiar_nombre(nombre)
            equipos_limpios = {self.limpiar_nombre(k): k for k in self.equipos_estandar.keys()}

            resultado = process.extractOne(
                nombre_limpio,
                equipos_limpios.keys(),
                scorer=fuzz.WRatio,
                score_cutoff=86
            )

            if resultado:
                mejor_limpio, puntuacion, _ = resultado
                mejor_match = equipos_limpios[mejor_limpio]
                self.cache_estandar[nombre] = mejor_match
                print(f"✔️ Coincidencia encontrada: '{nombre}' → '{mejor_match}' (puntuación: {puntuacion})")
            else:
                self.cache_estandar[nombre] = nombre
                print(f"❌ No se encontró coincidencia para: '{nombre}'")
                self.nombres_no_encontrados.add(nombre)
                ##self._agregar_a_json_principal(nombre)

        self.guardar_nombres_no_encontrados()
        return self.cache_estandar[nombre]

    def actualizar_partido(self, partido):
        home = self.estandarizar_nombre(partido["HomeTeam"])
        away = self.estandarizar_nombre(partido["AwayTeam"])
        clave = f"{home} vs {away}"

        if clave not in self.datos_por_partido:
            home_img = self._obtener_imagen_equipo(home)
            away_img = self._obtener_imagen_equipo(away)

            self.datos_por_partido[clave] = {
                "HomeTeam": home,
                "HomeTeamImg": home_img,
                "AwayTeam": away,
                "AwayTeamImg": away_img,
                "Time": partido.get("Time", ""),
                "Casinos": {}
            }

        self.datos_por_partido[clave]["Casinos"][partido["Casino"]] = {
            "1": float(partido["Odds1"]),
            "X": float(partido["OddsX"]),
            "2": float(partido["Odds2"]),
            "timestamp": time.time()
        }

        self._actualizar_mejores_cuotas(clave)

    def _actualizar_mejores_cuotas(self, clave_partido):
        partido = self.datos_por_partido[clave_partido]
        partido["Mejores_Cuotas"] = {}

        for mercado in ["1", "X", "2"]:
            todas_cuotas = [
                (casino, cuotas[mercado])
                for casino, cuotas in partido["Casinos"].items()
                if mercado in cuotas
            ]

            if not todas_cuotas:
                print(f"⚠️ No hay cuotas para el mercado '{mercado}' en '{clave_partido}'")
                continue

            mejor_casino, mejor_cuota = max(todas_cuotas, key=lambda x: x[1])
            partido["Mejores_Cuotas"][mercado] = {
                "cuota": mejor_cuota,
                "casino": mejor_casino
            }

    def detectar_surebets(self, clave_partido):
        partido = self.datos_por_partido.get(clave_partido)
        if not partido or "Mejores_Cuotas" not in partido:
            return None

        mejores = partido["Mejores_Cuotas"]
        mercados = ["1", "X", "2"]

        # Comprobar que todas las cuotas existen y son válidas
        try:
            cuotas = [float(mejores[m]["cuota"]) for m in mercados]
        except (KeyError, TypeError, ValueError):
            return None

        if any(c <= 0 for c in cuotas):
            return None

        arbitraje = sum(1 / c for c in cuotas)
        surebet = arbitraje < 1

        resultado = {
            "surebet": surebet,
            "arbitraje": round(arbitraje, 4),
            "cuotas": {m: mejores[m] for m in mercados}
        }

        if surebet:
            resultado["beneficio_estimado_%"] = round((1 - arbitraje) * 100, 2)

        return resultado
