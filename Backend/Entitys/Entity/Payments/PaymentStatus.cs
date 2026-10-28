namespace EskaCMS.Entities.Entity
{
    public enum PaymentStatus
    {
        Succeeded = 1,
        Initiated = 2,
        Failed = 5,
        OnHold = 3,
        Cancled = 4,
        Recurring=5

    }

	public enum PaymentType
	{
        None=0,
		DCMS = 2,
		CP = 1
		
	

	}

	public enum RefundPaymentStatus
    {
        Succeeded = 1,
        Failed = 2,
     


    }
}
