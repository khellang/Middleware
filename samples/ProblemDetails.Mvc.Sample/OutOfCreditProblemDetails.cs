namespace ProblemDetails.Mvc.Sample;

public class OutOfCreditProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
{
    public OutOfCreditProblemDetails()
    {
        Accounts = new List<string>();
    }

    public decimal Balance { get; set; }

    public ICollection<string> Accounts { get; }
}
