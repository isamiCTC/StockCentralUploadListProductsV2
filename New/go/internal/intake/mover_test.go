package intake

import (
	"os"
	"path/filepath"
	"testing"
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
	if filepath.ToSlash(got.ProcessedPath) != "C:/processed/342/sub1/sub2/catalog.xlsx" {
		t.Fatalf("ProcessedPath = %q", filepath.ToSlash(got.ProcessedPath))
	}
	if filepath.ToSlash(got.ResultsPath) != "C:/processed/342/sub1/sub2/catalog.result.xlsx" {
		t.Fatalf("ResultsPath = %q", filepath.ToSlash(got.ResultsPath))
	}
	if filepath.ToSlash(got.StructureErrPath) != "C:/processed/342/sub1/sub2/catalog.structure-errors.xlsx" {
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
	if _, err := os.Stat(job.ProcessedPath); err != nil {
		t.Fatalf("processed file does not exist: %v", err)
	}
	if _, err := os.Stat(job.ProcessingPath); !os.IsNotExist(err) {
		t.Fatalf("processing file should not exist after move, err=%v", err)
	}
}
