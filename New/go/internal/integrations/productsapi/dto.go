package productsapi

// Este archivo define los DTOs principales usados para hablar con la API
// de productos respetando el contrato observado en el servicio legacy.
//
// Acá modelamos solo lo que la V2 realmente necesita hoy para:
// - consultar productos
// - crear o actualizar productos
// - sincronizar imágenes

// Product representa el payload principal de producto usado por el legacy.
// El contrato original tiene más campos; por ahora modelamos el subconjunto
// necesario para la importación desde Excel.
type Product struct {
	Sku              string           `json:"Sku"`
	ProviderId       int              `json:"ProviderId"`
	Provider         string           `json:"Provider"`
	Name             string           `json:"Name"`
	Description      string           `json:"Description"`
	ShortDescription string           `json:"ShortDescription"`
	Stock            int              `json:"Stock"`
	Price            float64          `json:"Price"`
	ListPrice        float64          `json:"ListPrice"`
	NetPrice         float64          `json:"NetPrice"`
	Taxes            float64          `json:"Taxes"`
	Weight           float64          `json:"Weight"`
	Height           float64          `json:"Height"`
	Width            float64          `json:"Width"`
	Depth            float64          `json:"Depth"`
	Active           bool             `json:"Active"`
	Ean              string           `json:"Ean"`
	Brand            string           `json:"Brand"`
	CategoryBranch   []CategoryBranch `json:"CategoryBranch"`
}

// CategoryBranch representa la categoría final que la API espera recibir.
type CategoryBranch struct {
	Code string `json:"Code"`
	Name string `json:"Name"`
}

// ProductImage es el payload de imágenes observado en el legacy.
type ProductImage struct {
	Base64 string `json:"Base64"`
}
