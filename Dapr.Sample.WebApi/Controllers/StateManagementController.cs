﻿using Dapr.Sample.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Dapr.Sample.WebApi.Controllers
{
    /// <summary>
    /// Sample showing Dapr State management with controller.
    /// </summary>
    [ApiController]
    public class StateManagementController : ControllerBase
    {
        /// <summary>
        /// State client to interact with Dapr runtime.
        /// </summary>
        StateClient _stateClient;

        /// <summary>
        /// State store name.
        /// </summary>
        const string StoreName = "statestore";

        /// <summary>
        /// Controller Constructor
        /// </summary>
        /// <param name="stateClient"></param>
        public StateManagementController([FromServices] StateClient stateClient)
        {
            _stateClient = stateClient;
        }

        /// <summary>
        /// Gets the account information as specified by the id.
        /// </summary>
        /// <param name="account">Account information for the id from Dapr state store.</param>
        /// <returns>Account information.</returns>
        [HttpGet("{account}")]
        public ActionResult<Account> Get([FromState(StoreName)]StateEntry<Account> account)
        {
            if (account.Value is null)
            {
                return NotFound();
            }

            return account.Value;
        }

        /// <summary>
        /// Method for depositing to account as specified in transaction.
        /// </summary>
        /// <param name="transaction">Transaction info.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [Topic("deposit")]
        [HttpPost("deposit")]
        public async Task<ActionResult<Account>> Deposit(Transaction transaction)
        {
            var state = await _stateClient.GetStateEntryAsync<Account>(StoreName, transaction.Id);
            state.Value ??= new Account() { Id = transaction.Id, };
            state.Value.Balance += transaction.Amount;
            await state.SaveAsync();
            return state.Value;
        }

        /// <summary>
        /// Method for withdrawing from account as specified in transaction.
        /// </summary>
        /// <param name="transaction">Transaction info.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [Topic("withdraw")]
        [HttpPost("withdraw")]
        public async Task<ActionResult<Account>> Withdraw(Transaction transaction)
        {
            var state = await _stateClient.GetStateEntryAsync<Account>(StoreName, transaction.Id);

            if (state.Value == null)
            {
                return NotFound();
            }

            state.Value.Balance -= transaction.Amount;
            await state.SaveAsync();
            return state.Value;
        }
    }
}
