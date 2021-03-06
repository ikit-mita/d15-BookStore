﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using BookStore.BusinessLogic;
using BookStore.DataAccess;
using BookStore.DataAccess.Models;
using IkitMita;
using IkitMita.Mvvm.ViewModels;

namespace BookStore.ViewModels
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CreateOrderViewModel: ChildViewModelBase
    {
        private GetEmployeeModel _currentEmployee;
        private DelegateCommand<string> _searchBooksCommand;
        private ICollection<GetClientModel> _clients;
        private ICollection<SearchBookModel> _foundBooks;
        private ObservableCollection<SaveOrderedBookModel> _orderedBooks;
        private DelegateCommand<SearchBookModel> _selectBookCommand;
        private DelegateCommand<SaveOrderedBookModel> _unselectBookCommand;
        private DelegateCommand _saveOrderCommand;
        private string _errorMessage;

        public CreateOrderViewModel()
        {
            Title = "Создание заказов";
        }

        public ICollection<GetClientModel> Clients
        {
            get { return _clients; }
            set
            {
                _clients = value; 
                OnPropertyChanged();
            }
        }

        public GetClientModel SelectedClient { get; set; }

        public ICollection<SearchBookModel> FoundBooks
        {
            get { return _foundBooks; }
            set
            {
                _foundBooks = value; 
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SaveOrderedBookModel> OrderedBooks
        {
            get { return _orderedBooks; }
            set
            {
                _orderedBooks = value; 
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value; 
                OnPropertyChanged();
            }
        }

        public async void InitializeAsync()
        {
            using (StartOperation())
            {
                Clients = await GetClientOperation.ExecuteAsync();
                _currentEmployee = await GetEmployeeOperation.ExecuteAsync(SecurityManager.GetCurrentUser().Id);
                OrderedBooks = new ObservableCollection<SaveOrderedBookModel>();
            }
        }

        public DelegateCommand<string> SearchBooksCommand
            => _searchBooksCommand ?? (_searchBooksCommand = new DelegateCommand<string>(SearchBooksAsync));

        public DelegateCommand<SearchBookModel> SelectBookCommand
            => _selectBookCommand ?? (_selectBookCommand = new DelegateCommand<SearchBookModel>(SelectBook));

        public DelegateCommand<SaveOrderedBookModel> UnselectBookCommand
            => _unselectBookCommand 
            ?? (_unselectBookCommand = new DelegateCommand<SaveOrderedBookModel>(bm => OrderedBooks.Remove(bm)));

        public DelegateCommand SaveOrderCommand => _saveOrderCommand
            ?? (_saveOrderCommand = new DelegateCommand(SaveOrderAsync));

        private async void SaveOrderAsync()
        {
            if (SelectedClient == null)
            {
                ErrorMessage = "Не выбран клиент.";
                return;
            }

            if (OrderedBooks.IsNullOrEmpty())
            {
                ErrorMessage = "Необходимо добавить хотя бы 1 книгу.";
                return;
            }

            using (StartOperation())
            {
                var saveOrderModel = new SaveOrderModel
                {
                    OrderedBooks = OrderedBooks,
                    BranchId = _currentEmployee.BranchId,
                    ClientId = SelectedClient.Id,
                    EmployeeId = _currentEmployee.Id,
                    OrderDate = DateTime.Now
                };

                await SaveOrderOperation.ExecuteAsync(saveOrderModel);
                await Close(true);
            }
        }

        private void SelectBook(SearchBookModel bookModel)
        {
            var saveOrderedBookModel = OrderedBooks.FirstOrDefault(ob => ob.BookId == bookModel.BookId);

            if (saveOrderedBookModel == null)
            {
                saveOrderedBookModel = new SaveOrderedBookModel
                {
                    BookId = bookModel.BookId,
                    BookTitle = bookModel.BookTitle,
                    Amount = 0,
                    MaxAmount = bookModel.Amount,
                    Price = bookModel.Price
                };
                OrderedBooks.Add(saveOrderedBookModel);
            }

            saveOrderedBookModel.Amount += 1;
        }

        private async void SearchBooksAsync(string searchString)
        {
            using (StartOperation())
            {
                FoundBooks = await SearchBoksOperation.ExecuteAsync(searchString,_currentEmployee.BranchId);
            }
        }

        [Import]
        private IGetClientOperation GetClientOperation { get; set; }

        [Import]
        private ISearchBooksOperation SearchBoksOperation { get; set; }

        [Import]
        private ISecurityManager SecurityManager { get; set; }

        [Import]
        private IGetEmployeeOperation GetEmployeeOperation { get; set; }

        [Import]
        private ISaveOrderOperation SaveOrderOperation { get; set; }


    }


}

