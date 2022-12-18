using RangemanSync.ViewModels.Download;

namespace RangemanSync;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel viewModel;

    public MainPage(MainPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        this.viewModel = viewModel;
    }

    private void LogHeadersList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is LogHeaderViewModel selectedLogHeader)
        {
            viewModel.SelectedLogHeader = selectedLogHeader;
        }
    }
}

