package com.example.model

data class ServiceCategory(val id: Int, val name: String, val iconRes: Int? = null)
data class Worker(val id: String, val name: String, val rating: Double, val distance: String, val experience: String, val avatarUrl: String? = null, val matchPercentage: Int = 0, val reviews: Int = 0)
data class Booking(val id: String, val serviceName: String, val date: String, val time: String, val price: Double, val status: String, val worker: Worker? = null)

object MockData {
    val categories = listOf(
        ServiceCategory(1, "House Cleaning"),
        ServiceCategory(2, "Deep Cleaning"),
        ServiceCategory(3, "Sofa Cleaning"),
        ServiceCategory(4, "Carpet Cleaning"),
        ServiceCategory(5, "Office Cleaning"),
        ServiceCategory(6, "AC Cleaning")
    )

    val workers = listOf(
        Worker("w1", "Sarah Connor", 4.9, "1.2 km", "3 years", matchPercentage = 98, reviews = 420),
        Worker("w2", "John Smith", 4.6, "2.5 km", "1 year", matchPercentage = 85, reviews = 112),
        Worker("w3", "Maria Garcia", 4.8, "3.0 km", "5 years", matchPercentage = 95, reviews = 850)
    )
    
    val recommendedWorkers = workers.sortedByDescending { it.matchPercentage }.take(2)
}
