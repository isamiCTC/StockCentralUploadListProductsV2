using System;

namespace StockCentralToMagento.Entities
{
	public class MovementExEntity
	{
		private decimal		_MovementID;
		private int			_MovementTypeID;
		private int			_PointChargeID;
		private int			_CommerceID;
		private int			_StoreID;
		private int			_AccountID;
		private string		_CardID;
		private int			_PersonID;
		private int			_UserID;
		private DateTime	_CreationDate;
		private DateTime	_AuxDate;
		private decimal		_InitValue;
		private decimal		_EndValue;
		private decimal		_PointCount;
		private decimal		_MoneyCount;
		private string		_Remark;
		private string		_AditionalData;
		private string		_OperationCode;
		private string		_AmountPesos;
		private int		_Cuotas;
		private string		_MarcaTarjeta;
		private string		_Tarjeta;
		private decimal		_PointCountGlobal;
		private decimal		_MoneyCountGlobal;


		public MovementExEntity()
		{
			_MovementID=0;
			_MovementTypeID=0;
			_PointChargeID=-1;
			_CommerceID=-1;
			_StoreID=-1;
			_AccountID=-1;
			_CardID= null;
			_PersonID=-1;
			_UserID=-1;
			_CreationDate= DateTime.MaxValue;
			_AuxDate= DateTime.MaxValue;
			_InitValue=0;
			_EndValue=0;
			_PointCount=0;
			_MoneyCount=0;
			_Remark=null;
			_AditionalData=null;
			_AmountPesos = "0";
			_Cuotas = 0;
			_MarcaTarjeta="";
			_Tarjeta = "";
			_PointCountGlobal = 0;
			_MoneyCountGlobal = 0;

		}

		public MovementExEntity(int MovementTypeID,int PointChargeID, int CommerceID, int StoreID,
			int AccountID, string CardID,int PersonID ,int UserID ,DateTime CreationDate , DateTime AuxDate,
			decimal InitValue, decimal EndValue, decimal PointCount, decimal MoneyCount,
			string Remark, string AditionalData)
		{
			_MovementID=0;
			_MovementTypeID = MovementTypeID;
			_PointChargeID = PointChargeID;
			_CommerceID = CommerceID;
			_StoreID=StoreID;
			_AccountID=AccountID;
			_CardID= CardID;
			_PersonID=PersonID;
			_UserID=UserID;
			_CreationDate= CreationDate;
			_AuxDate= AuxDate;
			_InitValue=InitValue;
			_EndValue=EndValue;
			_PointCount=PointCount;
			_MoneyCount=MoneyCount;
			_Remark=Remark;
			_AditionalData=AditionalData;

		}

			public decimal MovementID
			{
				get{return _MovementID;}
				set{_MovementID  =value ;}
			}

			
			public int MovementTypeID
			{
				get{return _MovementTypeID;}
				set{_MovementTypeID  =value ;}
			}
		public int PointChargeID
		{
			get {return _PointChargeID;}
			set {_PointChargeID = value;}
		}
		public string OperationCode
		{
			get {return _OperationCode;}
			set {_OperationCode = value;}
		}
			public int CommerceID
			{
				get{return _CommerceID;}
				set{_CommerceID  =value ;}
			}

			
			public int StoreID
			{
				get{return _StoreID;}
				set{_StoreID  =value ;}
			}

		
			public int AccountID
			{
				get{return _AccountID;}
				set{_AccountID  =value ;}
			}

			
			public string CardID
			{
				get{return _CardID;}
				set{_CardID  =value ;}
			}

			
			public int PersonID
			{
				get{return _PersonID;}
				set{_PersonID  =value ;}
			}

			
			public int UserID
			{
				get{return _UserID;}
				set{_UserID  =value ;}
			}


			public DateTime  CreationDate
			{
				get{return _CreationDate;}
				set{_CreationDate  =value ;}
			}


			public DateTime AuxDate 
			{
				get{return _AuxDate;}
				set{_AuxDate  =value ;}
			}


			public decimal InitValue
			{
				get{return _InitValue;}
				set{_InitValue  =value ;}
			}


			public decimal EndValue
			{
				get{return _EndValue;}
				set{_EndValue  =value ;}
			}


			public decimal PointCount
			{
				get{return _PointCount;}
				set{_PointCount  =value ;}
			}


			public decimal MoneyCount
			{
				get{return _MoneyCount;}
				set{_MoneyCount  =value ;}
			}
			public string Remark
			{
				get{return _Remark;}
				set{_Remark  =value ;}
			}
			public string AditionalData
			{
				get{return _AditionalData;}
				set{_AditionalData  =value ;}
			}

			public int Cuotas
			{
				get { return _Cuotas; }
				set { _Cuotas = value; }
			}
		public string AmountPesos
		{
			get { return _AmountPesos; }
			set { _AmountPesos = value; }
		}
		public string MarcaTarjeta
		{
			get { return _MarcaTarjeta; }
			set { _MarcaTarjeta = value; }
		}
		public string Tarjeta
		{
			get { return _Tarjeta; }
			set { _Tarjeta = value; }
		}
		public decimal PointCountGlobal
		{
			get { return _PointCountGlobal; }
			set { _PointCountGlobal = value; }
		}


		public decimal MoneyCountGlobal
		{
			get { return _MoneyCountGlobal; }
			set { _MoneyCountGlobal = value; }
		}

	}

	public class RemoteMovementEntity
	{
		private string		_MovementID;
		private int			_MovementTypeID;
		private int			_PointChargeID;
		private string		_StoreName;
		private int			_AccountID;
		private string		_CardID;
		private string		_PersonFirstName;
		private string		_PersonLastName;
		private DateTime	_CreationDate;
		private DateTime	_AuxDate;
		private decimal		_InitValue;
		private decimal		_EndValue;
		private decimal		_PointCount;
		private decimal		_MoneyCount;
		private string		_Remark;
		private string		_AditionalData;
		private int			_SafState;

		public RemoteMovementEntity()
		{
			_MovementID="";
			_MovementID="";
			_PersonFirstName="";
			_PersonLastName="";
			_MovementTypeID=0;
			_PointChargeID=-1;
		
			_StoreName="";
			_AccountID=-1;
			_CardID= null;
		
		
			_CreationDate= DateTime.MaxValue;
			_AuxDate= DateTime.MaxValue;
			_InitValue=0;
			_EndValue=0;
			_PointCount=0;
			_MoneyCount=0;
			_Remark=null;
			_AditionalData=null;
			_SafState = 0;
		}

		public RemoteMovementEntity(int MovementTypeID,int PointChargeID, int CommerceID, int StoreID,
			int AccountID, string CardID,int PersonID ,int UserID ,DateTime CreationDate , DateTime AuxDate,
			decimal InitValue, decimal EndValue, decimal PointCount, decimal MoneyCount,
			string Remark, string AditionalData)
		{
			_MovementID="";
			_MovementTypeID = MovementTypeID;
			_PointChargeID = PointChargeID;
			//_StoreID=StoreID;
			_AccountID=AccountID;
			_CardID= CardID;	
			_CreationDate= CreationDate;
			_AuxDate= AuxDate;
			_InitValue=InitValue;
			_EndValue=EndValue;
			_PointCount=PointCount;
			_MoneyCount=MoneyCount;
			_Remark=Remark;
			_AditionalData=AditionalData;
		}

		public string MovementID
		{
			get{return _MovementID;}
			set{_MovementID  =value ;}
		}

			
		public int MovementTypeID
		{
			get{return _MovementTypeID;}
			set{_MovementTypeID  =value ;}
		}
		public int PointChargeID
		{
			get {return _PointChargeID;}
			set {_PointChargeID = value;}
		}
	
			
		public string StoreName
		{
			get{return _StoreName;}
			set{_StoreName  =value ;}
		}

		
		public int AccountID
		{
			get{return _AccountID;}
			set{_AccountID  =value ;}
		}

			
		public string CardID
		{
			get{return _CardID;}
			set{_CardID  =value ;}
		}

			
		public DateTime  CreationDate
		{
			get{return _CreationDate;}
			set{_CreationDate  =value ;}
		}


		public DateTime AuxDate 
		{
			get{return _AuxDate;}
			set{_AuxDate  =value ;}
		}


		public decimal InitValue
		{
			get{return _InitValue;}
			set{_InitValue  =value ;}
		}


		public decimal EndValue
		{
			get{return _EndValue;}
			set{_EndValue  =value ;}
		}


		public decimal PointCount
		{
			get{return _PointCount;}
			set{_PointCount  =value ;}
		}


		public decimal MoneyCount
		{
			get{return _MoneyCount;}
			set{_MoneyCount  =value ;}
		}
		public string Remark
		{
			get{return _Remark;}
			set{_Remark  =value ;}
		}
		public string AditionalData
		{
			get{return _AditionalData;}
			set{_AditionalData  =value ;}
		}

		public string PersonFirstName
		{
			get{return _PersonFirstName;}
			set{_PersonFirstName  =value ;}
		}
		public string PersonLastName
		{
			get{return _PersonLastName;}
			set{_PersonLastName  =value ;}
		}
		
		public int SAFState
		{
			get{return _SafState;}
			set{_SafState  =value ;}
		}
		
	}
}
