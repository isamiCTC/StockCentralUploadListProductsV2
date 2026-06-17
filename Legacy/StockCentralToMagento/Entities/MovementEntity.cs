using System;

namespace StockCentralToMagento.Entities
{
	public class MovementEntity
	{
		private decimal movimientoID;
		private int movimientoTipoID;
		private string sucursalOrigen;
		private string sucursalFinal;
		private string depositoOrigen;
		private string depositoFinal;
		private string articuloCodigo;
		private float inicial;
		private float final;
		private string cuentaID;
		private string descripcion;
		private int userID;
		//SAFnew agregado userName
		private string userName = string.Empty;
		private decimal operacionID;
		private string adicionalData;
		private float tckcount;
		private float moncount;
		private float millcount;
		private DateTime fechaAlta;
        private DateTime? fechaConcil;
        private string concilID;
        private int catalogId;

		public MovementEntity(int tipoID,string sucursalOrigen,string sucursalFinal,string depositoOrigen, string depositoFinal, string articuloCodigo,float inicial, float final, string cuentaID, string descripcion, int userID,decimal operacionID,string adicionalData, float tckcount, float moncount)
		{
			this.movimientoTipoID = tipoID;
			this.sucursalOrigen  = sucursalOrigen;
			this.sucursalFinal = sucursalFinal;
			this.depositoOrigen = depositoOrigen;
			this.depositoFinal = depositoFinal;
			this.articuloCodigo = articuloCodigo;
			this.inicial = inicial;
			this.final = final;
			this.cuentaID = cuentaID;
			this.descripcion = descripcion;
			this.userID = userID;
			this.operacionID = operacionID;
			this.adicionalData = adicionalData;
			this.tckcount = tckcount;
			this.moncount = moncount;
			//fechaAlta
			fechaAlta = new DateTime(System.DateTime.Now.Year,System.DateTime.Now.Month,System.DateTime.Now.Day); //Agregue esto para evitar bug
		}

		//Sobrecargo este metodo para pasarle la fecha - martin
		public MovementEntity(int tipoID,string sucursalOrigen,string sucursalFinal,string depositoOrigen, string depositoFinal, string articuloCodigo,float inicial, float final, string cuentaID, string descripcion, int userID,decimal operacionID,string adicionalData, float tckcount, float moncount,System.DateTime FecAlta)
		{
			this.movimientoTipoID = tipoID;
			this.sucursalOrigen  = sucursalOrigen;
			this.sucursalFinal = sucursalFinal;
			this.depositoOrigen = depositoOrigen;
			this.depositoFinal = depositoFinal;
			this.articuloCodigo = articuloCodigo;
			this.inicial = inicial;
			this.final = final;
			this.cuentaID = cuentaID;
			this.descripcion = descripcion;
			this.userID = userID;
			this.operacionID = operacionID;
			this.adicionalData = adicionalData;
			this.tckcount = tckcount;
			this.moncount = moncount;
			this.fechaAlta = FecAlta;
		}

		public MovementEntity(int tipoID,string sucursalOrigen,string sucursalFinal,string depositoOrigen, string depositoFinal, string articuloCodigo,float inicial, float final, string cuentaID, string descripcion, int userID,decimal operacionID,string adicionalData, float tckcount, float moncount, float millcount)
		{
			this.movimientoTipoID = tipoID;
			this.sucursalOrigen  = sucursalOrigen;
			this.sucursalFinal = sucursalFinal;
			this.depositoOrigen = depositoOrigen;
			this.depositoFinal = depositoFinal;
			this.articuloCodigo = articuloCodigo;
			this.inicial = inicial;
			this.final = final;
			this.cuentaID = cuentaID;
			this.descripcion = descripcion;
			this.userID = userID;
			this.operacionID = operacionID;
			this.adicionalData = adicionalData;
			this.tckcount = tckcount;
			this.moncount = moncount;
			this.millcount = millcount;
			//fechaAlta
			fechaAlta = new DateTime(System.DateTime.Now.Year,System.DateTime.Now.Month,System.DateTime.Now.Day);
		}

        public MovementEntity(int tipoID, string sucursalOrigen, string sucursalFinal, string depositoOrigen, string depositoFinal, string articuloCodigo, float inicial, float final, string cuentaID, string descripcion, int userID, decimal operacionID, string adicionalData, float tckcount, float moncount, float millcount, int catalogId)
        {
            this.movimientoTipoID = tipoID;
            this.sucursalOrigen = sucursalOrigen;
            this.sucursalFinal = sucursalFinal;
            this.depositoOrigen = depositoOrigen;
            this.depositoFinal = depositoFinal;
            this.articuloCodigo = articuloCodigo;
            this.inicial = inicial;
            this.final = final;
            this.cuentaID = cuentaID;
            this.descripcion = descripcion;
            this.userID = userID;
            this.operacionID = operacionID;
            this.adicionalData = adicionalData;
            this.tckcount = tckcount;
            this.moncount = moncount;
            this.millcount = millcount;
            this.CatalogId = catalogId;
            //fechaAlta
            fechaAlta = new DateTime(System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day);
        }
		
		//Sobrecargo este metodo para pasarle la fecha - martin
		public MovementEntity(int tipoID,string sucursalOrigen,string sucursalFinal,string depositoOrigen, string depositoFinal, string articuloCodigo,float inicial, float final, string cuentaID, string descripcion, int userID,decimal operacionID,string adicionalData, float tckcount, float moncount, float millcount, System.DateTime FecAlta)
		{
			this.movimientoTipoID = tipoID;
			this.sucursalOrigen  = sucursalOrigen;
			this.sucursalFinal = sucursalFinal;
			this.depositoOrigen = depositoOrigen;
			this.depositoFinal = depositoFinal;
			this.articuloCodigo = articuloCodigo;
			this.inicial = inicial;
			this.final = final;
			this.cuentaID = cuentaID;
			this.descripcion = descripcion;
			this.userID = userID;
			this.operacionID = operacionID;
			this.adicionalData = adicionalData;
			this.tckcount = tckcount;
			this.moncount = moncount;
			this.millcount = millcount;
			this.fechaAlta = FecAlta;
			//fechaAlta
		}

		public MovementEntity(int tipoID,string sucursalOrigen,string sucursalFinal,string depositoOrigen, string depositoFinal, string articuloCodigo,float inicial, float final, string cuentaID, string descripcion, int userID,decimal operacionID,string adicionalData, float tckcount, float moncount, float millcount, string userName)
		{
			this.movimientoTipoID = tipoID;
			this.sucursalOrigen  = sucursalOrigen;
			this.sucursalFinal = sucursalFinal;
			this.depositoOrigen = depositoOrigen;
			this.depositoFinal = depositoFinal;
			this.articuloCodigo = articuloCodigo;
			this.inicial = inicial;
			this.final = final;
			this.cuentaID = cuentaID;
			this.descripcion = descripcion;
			this.userID = userID;
			this.operacionID = operacionID;
			this.adicionalData = adicionalData;
			this.tckcount = tckcount;
			this.moncount = moncount;
			this.millcount = millcount;
			this.userName = userName;
			//fechaAlta
			fechaAlta = new DateTime(System.DateTime.Now.Year,System.DateTime.Now.Month,System.DateTime.Now.Day);
		}
		public MovementEntity()
		{
			movimientoID = -1;
			movimientoTipoID = -1;
			sucursalOrigen = String.Empty;
			sucursalFinal = String.Empty;
			depositoOrigen = String.Empty;
			depositoFinal = String.Empty;
			articuloCodigo = String.Empty;
			inicial = 0;
			final = 0;
			cuentaID = String.Empty;
			descripcion = String.Empty;
			userID = -1;
			userName = string.Empty;
			operacionID = -1m;
			adicionalData = String.Empty;
			tckcount = 0;
			moncount = 0;
			millcount = 0;
			fechaAlta = new DateTime(System.DateTime.Now.Year,System.DateTime.Now.Month,System.DateTime.Now.Day);
			//fechaAlta
		}


		public decimal MovimientoID
		{
			get {return movimientoID;}
			set {movimientoID = value;}
		}
		public int MovimientoTipoID
		{
			get {return movimientoTipoID;}
			set {movimientoTipoID = value;}
		}
		public string SucursalOrigen
		{
			get {return sucursalOrigen;}
			set {sucursalOrigen = value;}
		}
		public string SucursalFinal
		{
			get {return sucursalFinal;}
			set {sucursalFinal = value;}
		}
		public string DepositoOrigen
		{
			get {return depositoOrigen;}
			set {depositoOrigen = value;}
		}
		public string DepositoFinal
		{
			get {return depositoFinal;}
			set {depositoFinal = value;}
		}
		public string ArticuloCodigo
		{
			get {return articuloCodigo;}
			set {articuloCodigo = value;}
		}
		public DateTime FechaAlta
		{
			get {return fechaAlta;}
			set {fechaAlta = value;}
		}
		public float Inicial
		{
			get {return inicial;}
			set {inicial = value;}
		}
		public float Final
		{
			get {return final;}
			set {final = value;}
		}
		public string CuentaID
		{
			get {return cuentaID;}
			set {cuentaID = value;}
		}
		public string Descripcion
		{
			get {return descripcion;}
			set {descripcion = value;}
		}
		public int UserID
		{
			get {return userID;}
			set {userID = value;}
		}
		public string UserName
		{
			get {return userName;}
			set {userName = value;}
		}
		public decimal OperacionID
		{
			get {return operacionID;}
			set {operacionID = value;}
		}
		public string AdicionalData
		{
			get {return adicionalData;}
			set {adicionalData = value;}
		}
		public float TckCount
		{
			get {return tckcount;}
			set {tckcount = value;}
		}
		public float MonCount
		{
			get {return moncount;}
			set {moncount = value;}
		}
		public float MilesCount
		{
			get {return millcount;}
			set {millcount = value;}
		}
        public DateTime? FechaConcil
        {
            get { return fechaConcil; }
            set { fechaConcil = value; }
        }
        public string ConcilID
        {
            get { return concilID; }
            set { concilID = value; }
        }
        public int CatalogId
        {
            get { return catalogId; }
            set { catalogId = value; }
        }
 	}
}