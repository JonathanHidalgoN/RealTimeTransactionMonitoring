resource "azurerm_eventhub" "anomalies" {
  name                = "anomalies"
  namespace_name      = azurerm_eventhub_namespace.eh_namespace.name
  resource_group_name = azurerm_resource_group.rg.name
  partition_count     = 1
  message_retention   = 1
}

resource "azurerm_role_assignment" "app_identity_eh_anomalies_sender" {
  scope                = azurerm_eventhub.anomalies.id
  role_definition_name = "Azure Event Hubs Data Sender"
  principal_id         = azurerm_user_assigned_identity.app_identity.principal_id
}
