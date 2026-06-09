// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Requests.Update;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Abstractions
{
    /// <summary>
    /// Interface for the API.
    /// </summary>
    public interface IAPIClient
    {
        void SetBearerToken(string bearerToken);
        Task<AuthenticationModel?> Authorise();
        Task<(List<UserModel>, bool)> GetUsers();
        Task<(List<UserSettingModel>, bool)> GetUserSettings(int userId);
        Task<(List<ServerModel>, bool)> GetServers();
        Task<(List<EventModel>, bool)> GetServerEvents(List<KeyValuePair<string, object>> queryParameters);
        Task<(SettingModel?, ResponseModel?)> UpdateUserSettings(int userSettingId, UserSettingUpdateRequestModel userSetting);
        Task<(UserModel?, ResponseModel?)> UpdateUser(int userId, UserUpdateRequestModel user);
        Task<(AlertInformationModel?, bool)> GetAlerts(List<KeyValuePair<string, object>> queryParameters);
        Task<(AlertModel?, bool)> GetAlert(int alertId);
        Task<(AlertModel?, ResponseModel?)> UpdateAlert(int alertId, AlertUpdateRequestModel alert);
        Task<(AlertModel?, ResponseModel?)> RegisterAlert(AlertRequestModel alert);
        Task<(EventModel?, ResponseModel?)> RegisterServerEvent(EventRequestModel newEvent);
    }
}
