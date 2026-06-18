package main

import (
	"os"

	"stockcentraluploadlistproductsv2/internal/cli"
)

// Este archivo es la puerta de entrada más chica posible del binario.
//
// La idea es que `main` no tenga lógica de negocio ni decisiones complejas.
// Solo le pasa el control a la CLI y sale con el código que esa CLI devuelva.
func main() {
	os.Exit(cli.Execute())
}
