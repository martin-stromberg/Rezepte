using Rezepte.Models;
using System;
using System.Linq;

namespace Rezepte.Services
{
    public interface IReceiptSource
    {

        Task<ISourceReceipt> FromUriAsync(string uri);

    }

    public interface ISourceReceipt
    {

        Receipt ToModel();

    }
}
