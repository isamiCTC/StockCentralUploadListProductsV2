package logging

// Este archivo deja reservado un tipo de configuración local del paquete
// de logging. Hoy no está conectado al resto del sistema, pero sirve como
// lugar natural para futuras extensiones si más adelante queremos separar
// la configuración interna de la configuración global de la app.
type FileConfig struct {
	Path       string
	MaxSizeMB  int
	MaxBackups int
	MaxAgeDays int
}
