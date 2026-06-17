package logging

import (
	"fmt"
	"io"
	"strings"
	"sync"
	"time"
)

// Este archivo contiene el logger propio del proyecto.
//
// La idea es mantener una implementación sencilla y totalmente controlada:
// formato humano, timestamp fijo, levels claros y compatibilidad con los dos
// archivos de log que necesita el batch.
type Level string

const (
	LevelDebug Level = "DEBUG"
	LevelInfo  Level = "INFO"
	LevelWarn  Level = "WARN"
	LevelError Level = "ERROR"
)

// Field representa un par clave/valor para enriquecer una línea de log
// sin tener que construir strings manualmente en cada llamada.
type Field struct {
	Key   string
	Value any
}

// Helpers cortos para armar fields tipados de manera simple.
func String(key, value string) Field  { return Field{Key: key, Value: value} }
func Int(key string, value int) Field { return Field{Key: key, Value: value} }

// LoggerSet agrupa las dos salidas oficiales del proyecto.
type LoggerSet struct {
	Summary *Logger
	Detail  *Logger
}

// Logger es la unidad mínima de escritura.
// Tiene:
// - un nivel mínimo
// - un writer de salida
// - un mutex para evitar que dos goroutines mezclen líneas
type Logger struct {
	minLevel Level
	writer   io.Writer
	mu       sync.Mutex
}

// New crea una instancia simple de logger para un writer dado.
func New(minLevel Level, writer io.Writer) *Logger {
	return &Logger{
		minLevel: minLevel,
		writer:   writer,
	}
}

// Métodos públicos por nivel. Internamente todos delegan en `log`.
func (l *Logger) Debug(msg string, fields ...Field) { l.log(LevelDebug, msg, fields...) }
func (l *Logger) Info(msg string, fields ...Field)  { l.log(LevelInfo, msg, fields...) }
func (l *Logger) Warn(msg string, fields ...Field)  { l.log(LevelWarn, msg, fields...) }

func (l *Logger) Error(msg string, fields ...Field) {
	l.log(LevelError, msg, fields...)
}

// log aplica el filtro de nivel, formatea la línea y la escribe protegida
// por mutex para no mezclar salidas concurrentes.
func (l *Logger) log(level Level, msg string, fields ...Field) {
	if !enabled(l.minLevel, level) {
		return
	}

	line := formatLine(time.Now(), level, msg, fields...)

	l.mu.Lock()
	defer l.mu.Unlock()
	_, _ = io.WriteString(l.writer, line)
}

// enabled compara el nivel actual contra el nivel mínimo configurado.
func enabled(minLevel, current Level) bool {
	order := map[Level]int{
		LevelDebug: 1,
		LevelInfo:  2,
		LevelWarn:  3,
		LevelError: 4,
	}

	return order[current] >= order[minLevel]
}

// formatLine construye la línea final con el formato humano acordado.
// Ejemplo:
// 2026-06-16 13:40:00 INFO  file-start | provider_id=342 file=test.xlsx
func formatLine(ts time.Time, level Level, msg string, fields ...Field) string {
	var b strings.Builder
	b.WriteString(ts.Format("2006-01-02 15:04:05"))
	b.WriteString(" ")
	b.WriteString(fmt.Sprintf("%-5s", string(level)))
	b.WriteString(" ")
	b.WriteString(msg)

	if len(fields) > 0 {
		b.WriteString(" | ")
		for i, field := range fields {
			if i > 0 {
				b.WriteString(" ")
			}
			b.WriteString(field.Key)
			b.WriteString("=")
			b.WriteString(fmt.Sprintf("%v", field.Value))
		}
	}

	b.WriteString("\n")
	return b.String()
}
