package logging

import (
	"bytes"
	"strings"
	"testing"
)

func TestBufferFlushSeparatesBlockWithBlankLines(t *testing.T) {
	t.Parallel()

	var output bytes.Buffer
	logger := New(LevelDebug, &output)
	buffer := logger.NewBuffer()

	buffer.Info("sku-start")
	buffer.Info("sku-end")
	buffer.Flush()

	got := output.String()
	if !strings.HasPrefix(got, systemLineBreak) {
		t.Fatalf("output should start with a blank line, got %q", got)
	}
	if !strings.HasSuffix(got, systemLineBreak+systemLineBreak) {
		t.Fatalf("output should end with a blank line, got %q", got)
	}
	if !strings.Contains(got, "sku-start") || !strings.Contains(got, "sku-end") {
		t.Fatalf("output = %q, want both buffered lines", got)
	}
}

func TestLoggerBlankWritesSingleEmptyLine(t *testing.T) {
	t.Parallel()

	var output bytes.Buffer
	logger := New(LevelDebug, &output)

	logger.Blank()

	if got := output.String(); got != systemLineBreak {
		t.Fatalf("Blank() = %q, want newline", got)
	}
}
