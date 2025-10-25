//using Dapr.Client;
//using Moq;

//public class DaprDistributedCacheTests
//    {
//        [Fact]
//        public async Task GetAsync_ShouldCallDaprGetStateAsyncAndReturnData()
//        {
//            // Arrange
//            var mockDaprClient = new Mock<DaprClient>();
//            var storeName = "testStore";
//            var testKey = "user123";
//            var expectedData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

//            // Configura el mock para devolver el array de bytes cuando se llame
//            mockDaprClient
//                .Setup(c => c.GetStateAsync<byte[]>(
//                    storeName,
//                    testKey,
//                    It.IsAny<StateOptions>(), // Permite cualquier StateOptions (usualmente null)
//                    It.IsAny<System.Threading.CancellationToken>()))
//                .ReturnsAsync(expectedData);

//            var cache = new DaprDistributedCache(mockDaprClient.Object, storeName);

//            // Act
//            var actualData = await cache.GetAsync(testKey);

//            // Assert
//            Assert.Equal(expectedData, actualData);

//            // Verifica que el método del mock fue llamado exactamente una vez con los argumentos correctos
//            mockDaprClient.Verify(c => c.GetStateAsync<byte[]>(
//                storeName,
//                testKey,
//                null,
//                It.IsAny<System.Threading.CancellationToken>()),
//                Times.Once);
//        }
 