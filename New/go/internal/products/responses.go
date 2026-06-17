package products

// Este archivo modela las respuestas mínimas que necesitamos interpretar
// desde la API legacy.
//
// No intentamos tipar absolutamente todo. Solo lo suficiente para detectar
// las condiciones que hoy gobiernan el flujo del servicio original.

// ProductEnvelope representa respuestas del estilo `{ "Result": { ... } }`.
type ProductEnvelope struct {
	Result Product `json:"Result"`
}

// ProductErrorEnvelope representa respuestas donde `Result` trae
// una descripción de error.
type ProductErrorEnvelope struct {
	Result struct {
		Description string `json:"Description"`
	} `json:"Result"`
}

// ImageEnvelope representa la respuesta de consulta de imagen individual.
type ImageEnvelope struct {
	Result struct {
		Base64 string `json:"Base64"`
	} `json:"Result"`
}

// TransactionEnvelope representa respuestas que exponen `TransactionId`
// como parte de la señal de negocio.
type TransactionEnvelope struct {
	TransactionId string `json:"TransactionId"`
}
