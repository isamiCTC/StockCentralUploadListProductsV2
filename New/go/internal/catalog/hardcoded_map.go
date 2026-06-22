package catalog

import "stockcentraluploadlistproductsv2/internal/products"

// Este archivo guarda el mapeo hardcodeado de categorías heredado del legacy.
//
// El servicio original hace un `switch` sobre la columna `SUB CATEGORIA`.
// Acá conservamos ese conocimiento, pero en una estructura más simple
// de mantener y reutilizar.

var hardcodedBranches = map[string]products.CategoryBranch{
	normalizeCategoryKey("ACCESORIOS CEL"):             {Code: "1217", Name: "ACCESORIOS CEL"},
	normalizeCategoryKey("AUDIO"):                      {Code: "1211", Name: "Audio y Video"},
	normalizeCategoryKey("CELULARES"):                  {Code: "1217", Name: "Telefonía y Accesorios"},
	normalizeCategoryKey("CLIMATIZACIÓN"):              {Code: "1215", Name: "Línea Blanca y Climatización"},
	normalizeCategoryKey("COMPUTACIÓN"):                {Code: "1213", Name: "Tecnología y Computación"},
	normalizeCategoryKey("GAMING"):                     {Code: "1214", Name: "GAMING"},
	normalizeCategoryKey("ILUMINACIÓN"):                {Code: "1249", Name: "1249"},
	normalizeCategoryKey("LINEA BLANCA"):               {Code: "1215", Name: "LINEA BLANCA"},
	normalizeCategoryKey("MOVILIDAD"):                  {Code: "1226", Name: "Outdoors"},
	normalizeCategoryKey("PEQUEÑOS ELECTRODOMESTICOS"): {Code: "1212", Name: "Pequeños Electro"},
	normalizeCategoryKey("SALUD"):                      {Code: "1234", Name: "Cuidado Personal"},
	normalizeCategoryKey("TV"):                         {Code: "1214", Name: "TV y Gaming"},
	normalizeCategoryKey("MAQUILLAJE Y SKINCARE"):      {Code: "1235", Name: "Maquillaje y Skincare"},
	normalizeCategoryKey("PEQUEÑOS ELECTRO"):           {Code: "1212", Name: "Pequeños Electro"},
	normalizeCategoryKey("COCINA"):                     {Code: "1246", Name: "Cocina"},
	normalizeCategoryKey("HERRAMIENTAS"):               {Code: "1248", Name: "Herramientas"},
	normalizeCategoryKey("ACCESORIOS NIÑOS"):           {Code: "1218", Name: "Accesorios Niños"},
	normalizeCategoryKey("JUEGOS Y JUGUETES"):          {Code: "1220", Name: "Juegos y Juguetes"},
	normalizeCategoryKey("OUTDOORS"):                   {Code: "1226", Name: "Outdoors"},
	normalizeCategoryKey("ACCESORIOS DE VIAJES"):       {Code: "1221", Name: "Accesorios de Viajes"},
	normalizeCategoryKey("ACCESORIOS MASCOTAS"):        {Code: "1237", Name: "Accesorios Mascotas"},
}

var fallbackBranch = products.CategoryBranch{
	Code: "1041",
	Name: "Varios",
}
