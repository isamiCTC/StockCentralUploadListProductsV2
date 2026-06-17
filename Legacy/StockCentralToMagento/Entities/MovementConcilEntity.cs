using System;

namespace StockCentralToMagento.Entities
{
	/// <summary>
	/// Descripción breve de MovementConcilEntity.
	/// agregado 15/05/2008
	/// campo string userName
	/// </summary>
	public class MovementConcilEntity
	{
		private decimal movimientoID;
		private int movimientoTipoID;
		private string sucursalOrigen;
		private string sucursal;
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
		private float mlscount;
		private DateTime fechaAlta;
		private DateTime fechaconcil;
		private string concilid;
		
		private decimal cost_FIFO;
		private decimal cost_PPP;
		private decimal sale_Amount;
//		private int tck_Amount; 
//		private int mls_Amount;
        private string supplier_name;

		public MovementConcilEntity(int tipoID,string sucursalOrigen,string sucursalFinal,string depositoOrigen, string depositoFinal, string articuloCodigo,float inicial, float final, string cuentaID, string descripcion, int userID,string userName,decimal operacionID,string adicionalData, float tckcount, float moncount, float mlscount,string concilid, string sucursal )
		{
			this.movimientoTipoID = tipoID;
			this.sucursal  = sucursal;
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
			this.userName = userName;
			this.operacionID = operacionID;
			this.adicionalData = adicionalData;
			this.tckcount = tckcount;
			this.moncount = moncount;
			this.concilid = concilid;
			this.mlscount = mlscount;
			this.cost_FIFO = 0;
			this.cost_PPP = 0;
			this.sale_Amount = 0;
//			this.tck_Amount = 0; 
//			this.mls_Amount = 0;
			this.supplier_name = "";
		}
		public MovementConcilEntity(int tipoID,string sucursalOrigen,string sucursalFinal,
			string depositoOrigen, string depositoFinal, string articuloCodigo,float inicial, 
			float final, string cuentaID, string descripcion, int userID,string userName,decimal operacionID, 
			string adicionalData, float tckcount, float moncount, float mlscount,
			string concilid, string sucursal,decimal cost_FIFO, decimal cost_PPP, 
			decimal sale_Amount, int tck_Amount, int mls_Amount, string supplier_name)
		{
			this.movimientoTipoID = tipoID;
			this.sucursal  = sucursal;
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
			this.userName = userName;
			this.operacionID = operacionID;
			this.adicionalData = adicionalData;
			this.tckcount = tckcount;
			this.moncount = moncount;
			this.concilid = concilid;
			this.mlscount = mlscount;
			this.cost_FIFO = cost_FIFO;
			this.cost_PPP = cost_PPP;
			this.sale_Amount = sale_Amount;
//			this.tck_Amount = tck_Amount; 
//			this.mls_Amount = mls_Amount;
			this.supplier_name = supplier_name;
		}
		public MovementConcilEntity(int tipoID,string sucursalOrigen,string sucursalFinal,string depositoOrigen, string depositoFinal, string articuloCodigo,float inicial, float final, string cuentaID, string descripcion, int userID,decimal operacionID,string adicionalData, float tckcount, float moncount, float mlscount,string concilid, string sucursal )
		{
			this.movimientoTipoID = tipoID;
			this.sucursal  = sucursal;
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
			this.concilid = concilid;
			this.mlscount = mlscount;
			this.cost_FIFO = 0;
			this.cost_PPP = 0;
			this.sale_Amount = 0;
			//			this.tck_Amount = 0; 
			//			this.mls_Amount = 0;
			this.supplier_name = "";
		}
		public MovementConcilEntity()
		{
			movimientoID = -1;
			movimientoTipoID = -1;
			sucursal = String.Empty;
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
			adicionalData = String.Empty;
			tckcount = 0;
			moncount = 0;
			mlscount = 0;
			concilid= String.Empty;
			fechaAlta=new DateTime(1980,1,1);
			fechaconcil=new DateTime(1980,1,1);
			cost_FIFO = 0;
			cost_PPP = 0;
			sale_Amount = 0;
//			tck_Amount = 0; 
//			mls_Amount = 0;
			supplier_name = string.Empty;
		}

		public string ConcilID
		{
			get {return concilid;}
			set {concilid = value;}
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
		public string Sucursal
		{
			get {return sucursal;}
			set {sucursal = value;}
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
		public DateTime FechaConcil
		{
			get {return fechaconcil;}
			set {fechaconcil = value;}
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
		public float MlsCount
		{
			get {return mlscount;}
			set {mlscount = value;}
		}
		//
//		public int Mls_Amount
//		{
//			get {return mls_Amount;}
//			set {mls_Amount = value;}
//		}
//		public int Tck_Amount
//		{
//			get {return tck_Amount;}
//			set {tck_Amount = value;}
//		}
		public decimal Cost_FIFO
		{
			get {return cost_FIFO;}
			set {cost_FIFO = value;}
		}
		public decimal Cost_PPP
		{
			get {return cost_PPP;}
			set {cost_PPP = value;}
		}
		public decimal Sale_Amount
		{
			get {return sale_Amount;}
			set {sale_Amount = value;}
		}
		public string Supplier_name
		{
			get {return supplier_name;}
			set {supplier_name = value;}
		}
	}
}
