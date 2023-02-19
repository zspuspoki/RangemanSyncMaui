using RangemanSync.ViewModels.Config;

namespace RangemanSync;

public partial class ConfigPage : ContentPage
{
	public ConfigPage(ConfigPageViewModel configPageViewModel)
	{
		InitializeComponent();
		BindingContext = configPageViewModel;
	}
}