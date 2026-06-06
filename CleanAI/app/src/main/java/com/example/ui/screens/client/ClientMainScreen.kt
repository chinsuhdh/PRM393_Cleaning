package com.example.ui.screens.client

import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Chat
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.List
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector

enum class ClientTab(val label: String, val icon: ImageVector) {
    Home("Home", Icons.Default.Home),
    Bookings("Bookings", Icons.Default.List),
    Chat("AI Chat", Icons.Default.Chat),
    Notifications("Alerts", Icons.Default.Notifications),
    Profile("Profile", Icons.Default.Person)
}

@Composable
fun ClientMainScreen() {
    var selectedTab by remember { mutableStateOf(ClientTab.Home) }

    Scaffold(
        bottomBar = {
            NavigationBar {
                ClientTab.values().forEach { tab ->
                    NavigationBarItem(
                        icon = { Icon(tab.icon, contentDescription = tab.label) },
                        label = { Text(tab.label) },
                        selected = selectedTab == tab,
                        onClick = { selectedTab = tab }
                    )
                }
            }
        }
    ) { innerPadding ->
        Modifier.padding(innerPadding)
        
        when (selectedTab) {
            ClientTab.Home -> HomeScreen()
            ClientTab.Bookings -> BookingsScreen()
            ClientTab.Chat -> ChatScreen()
            ClientTab.Notifications -> NotificationsScreen()
            ClientTab.Profile -> ProfileScreen()
        }
    }
}
