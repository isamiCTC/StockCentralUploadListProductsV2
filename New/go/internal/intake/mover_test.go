package intake

import (
	"errors"
	"os"
	"path/filepath"
	"testing"
	"time"
)

// Este archivo prueba las reglas de rutas y movimientos del intake.
//
// La suite valida cómo se construyen paths derivados y cómo se trasladan
// archivos entre input, processing y processed.

func TestBuildPathsPreservesProviderAndRelativeStructure(t *testing.T) {
	t.Parallel()

	mover := NewMover("C:/processing", "C:/processed")
	job := FileJob{
		ProviderID:   342,
		RelativePath: filepath.Join("sub1", "sub2", "catalog.xlsx"),
	}

	got := mover.BuildPaths(job)

	if filepath.ToSlash(got.ProcessingPath) != "C:/processing/342/sub1/sub2/catalog.xlsx" {
		t.Fatalf("ProcessingPath = %q", filepath.ToSlash(got.ProcessingPath))
	}
	if got.ProcessedPath != "" {
		t.Fatalf("ProcessedPath = %q", filepath.ToSlash(got.ProcessedPath))
	}
	if got.ResultsPath != "" {
		t.Fatalf("ResultsPath = %q", filepath.ToSlash(got.ResultsPath))
	}
	if got.StructureErrPath != "" {
		t.Fatalf("StructureErrPath = %q", filepath.ToSlash(got.StructureErrPath))
	}
}

func TestMoveToProcessingAndProcessedMovesFileAndUpdatesInputPath(t *testing.T) {
	t.Parallel()

	root := t.TempDir()
	processingRoot := filepath.Join(root, "processing")
	processedRoot := filepath.Join(root, "processed")
	inputPath := filepath.Join(root, "input", "342", "catalog.xlsx")

	if err := os.MkdirAll(filepath.Dir(inputPath), 0o755); err != nil {
		t.Fatalf("MkdirAll input dir: %v", err)
	}
	if err := os.WriteFile(inputPath, []byte("content"), 0o600); err != nil {
		t.Fatalf("WriteFile input: %v", err)
	}

	mover := NewMover(processingRoot, processedRoot)
	mover.now = func() time.Time {
		return time.Date(2026, 6, 26, 14, 35, 22, 0, time.UTC)
	}
	job := mover.BuildPaths(FileJob{
		ProviderID:   342,
		InputPath:    inputPath,
		RelativePath: "catalog.xlsx",
	})

	job, err := mover.MoveToProcessing(job)
	if err != nil {
		t.Fatalf("MoveToProcessing returned error: %v", err)
	}
	if job.InputPath != job.ProcessingPath {
		t.Fatalf("InputPath after processing = %q, want %q", job.InputPath, job.ProcessingPath)
	}
	if _, err := os.Stat(job.ProcessingPath); err != nil {
		t.Fatalf("processing file does not exist: %v", err)
	}
	if _, err := os.Stat(inputPath); !os.IsNotExist(err) {
		t.Fatalf("original input file should not exist after move, err=%v", err)
	}

	job, err = mover.MoveToProcessed(job)
	if err != nil {
		t.Fatalf("MoveToProcessed returned error: %v", err)
	}
	if job.InputPath != job.ProcessedPath {
		t.Fatalf("InputPath after processed = %q, want %q", job.InputPath, job.ProcessedPath)
	}
	if filepath.ToSlash(job.ProcessedPath) != filepath.ToSlash(filepath.Join(processedRoot, "342", "catalog__20260626_143522.xlsx")) {
		t.Fatalf("ProcessedPath = %q", filepath.ToSlash(job.ProcessedPath))
	}
	if filepath.ToSlash(job.ResultsPath) != filepath.ToSlash(filepath.Join(processedRoot, "342", "catalog__20260626_143522.result.xlsx")) {
		t.Fatalf("ResultsPath = %q", filepath.ToSlash(job.ResultsPath))
	}
	if filepath.ToSlash(job.StructureErrPath) != filepath.ToSlash(filepath.Join(processedRoot, "342", "catalog__20260626_143522.structure-errors.xlsx")) {
		t.Fatalf("StructureErrPath = %q", filepath.ToSlash(job.StructureErrPath))
	}
	if _, err := os.Stat(job.ProcessedPath); err != nil {
		t.Fatalf("processed file does not exist: %v", err)
	}
	if _, err := os.Stat(job.ProcessingPath); !os.IsNotExist(err) {
		t.Fatalf("processing file should not exist after move, err=%v", err)
	}
}

func TestMoveToProcessingFallsBackToCopyAndRemoveOnCrossDeviceError(t *testing.T) {
	t.Parallel()

	root := t.TempDir()
	processingRoot := filepath.Join(root, "processing")
	processedRoot := filepath.Join(root, "processed")
	inputPath := filepath.Join(root, "input", "342", "catalog.xlsx")

	if err := os.MkdirAll(filepath.Dir(inputPath), 0o755); err != nil {
		t.Fatalf("MkdirAll input dir: %v", err)
	}
	if err := os.WriteFile(inputPath, []byte("content"), 0o600); err != nil {
		t.Fatalf("WriteFile input: %v", err)
	}

	mover := NewMover(processingRoot, processedRoot)
	mover.now = func() time.Time {
		return time.Date(2026, 6, 26, 14, 35, 22, 0, time.UTC)
	}
	mover.renameFile = func(oldPath, newPath string) error {
		return errors.New("rename C:\\source D:\\dest: El sistema no puede mover el archivo a otra unidad de disco.")
	}

	job := mover.BuildPaths(FileJob{
		ProviderID:   342,
		InputPath:    inputPath,
		RelativePath: "catalog.xlsx",
	})

	job, err := mover.MoveToProcessing(job)
	if err != nil {
		t.Fatalf("MoveToProcessing returned error: %v", err)
	}
	if job.InputPath != job.ProcessingPath {
		t.Fatalf("InputPath after processing = %q, want %q", job.InputPath, job.ProcessingPath)
	}

	content, err := os.ReadFile(job.ProcessingPath)
	if err != nil {
		t.Fatalf("ReadFile processing: %v", err)
	}
	if string(content) != "content" {
		t.Fatalf("processing content = %q, want %q", string(content), "content")
	}
	if _, err := os.Stat(inputPath); !os.IsNotExist(err) {
		t.Fatalf("original input file should not exist after fallback move, err=%v", err)
	}
}
