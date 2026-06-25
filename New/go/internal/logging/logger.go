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

// Buffer acumula varias líneas con el mismo formato del logger y luego las
// escribe de una sola vez. Esto sirve cuando queremos que un bloque completo
// quede junto en el archivo final, sin mezclarse con líneas de otra goroutine.
type Buffer struct {
	logger *Logger
	lines  []string
}

// New crea una instancia simple de logger para un writer dado.
func New(minLevel Level, writer io.Writer) *Logger {
	return &Logger{
		minLevel: minLevel,
		writer:   writer,
	}
}

// NewBuffer crea un acumulador temporal de líneas para este logger.
// El caller puede ir agregando eventos y luego hacer `Flush()` para que
// salgan juntos como un bloque atómico.
func (l *Logger) NewBuffer() *Buffer {
	return &Buffer{
		logger: l,
		lines:  make([]string, 0, 16),
	}
}

// Métodos públicos por nivel. Internamente todos delegan en `log`.
func (l *Logger) Debug(msg string, fields ...Field) { l.log(LevelDebug, msg, fields...) }
func (l *Logger) Info(msg string, fields ...Field)  { l.log(LevelInfo, msg, fields...) }
func (l *Logger) Warn(msg string, fields ...Field)  { l.log(LevelWarn, msg, fields...) }

func (l *Logger) Error(msg string, fields ...Field) {
	l.log(LevelError, msg, fields...)
}

// Blank deja una línea vacía explícita en el destino de log.
func (l *Logger) Blank() {
	if l == nil || l.writer == nil {
		return
	}

	l.mu.Lock()
	defer l.mu.Unlock()
	_, _ = io.WriteString(l.writer, "\n")
}

// log aplica el filtro de nivel, formatea la línea y la escribe protegida
// por mutex para no mezclar salidas concurrentes.
func (l *Logger) log(level Level, msg string, fields ...Field) {
	// Si el nivel no pasa el filtro configurado, cortamos acá y evitamos
	// trabajo innecesario de formateo/escritura.
	if !enabled(l.minLevel, level) {
		return
	}

	line := formatLine(time.Now(), level, msg, fields...)

	// El mutex evita que dos goroutines mezclen texto dentro de una misma línea.
	l.mu.Lock()
	defer l.mu.Unlock()
	_, _ = io.WriteString(l.writer, line)
}

// Métodos públicos del buffer por nivel. Mantienen la misma interfaz básica
// del logger normal, pero todavía no escriben nada al destino final.
func (b *Buffer) Debug(msg string, fields ...Field) { b.log(LevelDebug, msg, fields...) }
func (b *Buffer) Info(msg string, fields ...Field)  { b.log(LevelInfo, msg, fields...) }
func (b *Buffer) Warn(msg string, fields ...Field)  { b.log(LevelWarn, msg, fields...) }
func (b *Buffer) Error(msg string, fields ...Field) { b.log(LevelError, msg, fields...) }

// Flush escribe todo el bloque junto y luego vacía el buffer para poder
// reutilizarlo si hace falta.
func (b *Buffer) Flush() {
	if b == nil || b.logger == nil || len(b.lines) == 0 {
		return
	}

	var block strings.Builder
	block.WriteString("\n")
	for _, line := range b.lines {
		block.WriteString(line)
	}
	block.WriteString("\n")

	b.logger.mu.Lock()
	defer b.logger.mu.Unlock()
	_, _ = io.WriteString(b.logger.writer, block.String())
	b.lines = b.lines[:0]
}

// log formatea una línea con timestamp y fields, igual que el logger normal,
// pero la deja en memoria hasta que llegue el momento de hacer Flush.
func (b *Buffer) log(level Level, msg string, fields ...Field) {
	if b == nil || b.logger == nil {
		return
	}
	if !enabled(b.logger.minLevel, level) {
		return
	}

	b.lines = append(b.lines, formatLine(time.Now(), level, msg, fields...))
}

// enabled compara el nivel actual contra el nivel mínimo configurado.
func enabled(minLevel, current Level) bool {
	order := map[Level]int{
		LevelDebug: 1,
		LevelInfo:  2,
		LevelWarn:  3,
		LevelError: 4,
	}

	// Cuanto mayor es el número, más importante es el nivel.
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
			// Separamos fields con espacios para que sigan siendo legibles pero
			// queden en una sola línea fácil de filtrar.
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
