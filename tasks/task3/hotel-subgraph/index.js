import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import gql from 'graphql-tag';
import DataLoader from 'dataloader'; 

// Имитация данных (замените на реальную БД/gRPC позже)
const hotels = [
  { id: "h1", name: "Grand Hyatt", city: "New York", stars: 5 },
  { id: "h2", name: "Budget Inn", city: "Springfield", stars: 2 },
  { id: "h3", name: "Motel 6", city: "LA", stars: 1 },
];

const batchHotels = async (ids) => {
  console.log(`[DataLoader] Executing single batch request for IDs: ${ids.join(', ')}`);
  
  const dbHotels = hotels.filter(h => ids.includes(h.id));

  // DataLoader требует, чтобы результат был отсортирован в порядке исходных ID
  const sortedHotels = ids.map(id => dbHotels.find(hotel => hotel.id === id));
  
  return sortedHotels;
};

const typeDefs = gql`
extend schema
  @link(url: "https://specs.apollo.dev/federation/v2.7",
        import: ["@key", "@shareable", "@inaccessible", "@override", "@requires", "@external"])

  type Hotel @key(fields: "id") {
    id: ID!
    name: String
    city: String
    stars: Int
  }

  type Query {
    hotelsByIds(ids: [ID!]!): [Hotel]
  }
`;

const resolvers = {
  Hotel: {
    __resolveReference: async ({ id }, {dataLoaders}) => {
      // TODO: Реальный вызов к hotel-сервису или заглушка
      console.log(`[Hotel Service] Resolving reference for hotel ID: ${id}`);
      return dataLoaders.hotelLoader.load(id);
    },
  },
  Query: {
    hotelsByIds: async (_, { ids }) => {
      // TODO: Заглушка или REST-запрос
      return dataLoaders.hotelLoader.loadMany(ids);
    },
  },
};

const server = new ApolloServer({
  schema: buildSubgraphSchema([{ typeDefs, resolvers }]),
});

startStandaloneServer(server, {
  listen: { port: 4002 },
  context: async ({req}) => {
    
    // Создаем экземпляр DataLoader'а в контексте КАЖДОГО запроса клиента
    const hotelLoader = new DataLoader(batchHotels);
    
    return { 
      headers: req.headers,
      dataLoaders: { // Добавляем загрузчик в объект dataLoaders в контексте
        hotelLoader 
      }
    };

  }
}).then(() => {
  console.log('✅ Hotel subgraph ready at http://localhost:4002/');
});
