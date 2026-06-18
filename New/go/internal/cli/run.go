package cli

import (
	"context"

	"stockcentraluploadlistproductsv2/internal/app/runbatch"

	"github.com/spf13/cobra"
)

// Este archivo declara el subcomando `run`.
//
// Su trabajo es mínimo:
// - tomar los flags ya resueltos por Cobra;
// - pedir la ejecución del batch;
// - y convertir el código de salida en un error controlado para la CLI.
func newRunCmd(opts *options) *cobra.Command {
	return &cobra.Command{
		Use:   "run",
		Short: "Run the batch process",
		RunE: func(cmd *cobra.Command, args []string) error {
			// Usamos un contexto base simple. Más adelante, si hiciera falta,
			// acá podría agregarse cancelación, timeout global o señales del SO.
			if exitCode := runbatch.Execute(context.Background(), opts.settingsPath, opts.envPath); exitCode != 0 {
				return commandExitError{code: exitCode}
			}
			return nil
		},
	}
}
