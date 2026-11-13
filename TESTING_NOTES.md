# TESTING_NOTES

1. **Tests:** Total: <22>, Passing: <22>, Failing: 0.

2. **Line coverage:** 90.5% overall (Domain 100%, Application 95.8%, Infrastructure 0%), from `coverage-report/Summary.txt`.

3. **Libraries:**
   - **Moq:** used to mock `IOrderRepository`, `IInventoryService` and `INotificationService` when testing `CheckoutService`.
   - **AutoFixture:** <AutoFixture: used in CheckoutAsync_WithMultipleOrders_ReturnsSumOfAllOrders to generate 3 random Order objects (with random quantities and prices).>.
   - **Bogus:** <Bogus: Used the Bogus library to generate realistic and deterministic test data for Order and OrderItem objects.>.

4. **Fairness (Examples B and C):** < No, because the discount threshold creates a sudden price difference between two nearly identical purchases>

5. **Bugs found:** <none>