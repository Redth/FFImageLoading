﻿using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamvvm;

namespace Sample
{
	public partial class StreamListPage : ContentPage
	{
		StreamListPageModel viewModel;

		public StreamListPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new StreamListPageModel();
		}

		void ListView_ItemSelected(System.Object sender, Microsoft.Maui.Controls.SelectedItemChangedEventArgs e)
		{
			viewModel.ItemSelected();
		}
	}
}
