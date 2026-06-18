package selfcheck

import "io"

// Este archivo define el caso de uso "self-check".
//
// La capa CLI no necesita saber cómo se hacen los chequeos.
// Solo le pide a esta capa un exit code y esta capa se encarga del resto.
// Execute corre el self-check y traduce el resultado a código de salida.
func Execute(settingsPath, envPath string, out io.Writer) int {
	if err := Run(settingsPath, envPath, out); err != nil {
		return 1
	}

	return 0
}
