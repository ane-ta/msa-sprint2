import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import gql from 'graphql-tag';

// Имитация данных (замените на реальную БД/gRPC позже)
const hotels = [
  { id: "h1", name: "Grand Hyatt", city: "New York", stars: 5 },
  { id: "h2", name: "Budget Inn", city: "Springfield", stars: 2 },
];

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
    __resolveReference: async ({ id }) => {
      // TODO: Реальный вызов к hotel-сервису или заглушка
      console.log(`[Hotel Service] Resolving reference for hotel ID: ${id}`);
      return hotels.find(h => h.id === id);
    },
  },
  Query: {
    hotelsByIds: async (_, { ids }) => {
      // TODO: Заглушка или REST-запрос
      return hotels.filter(h => ids.includes(h.id));
    },
  },
};

const server = new ApolloServer({
  schema: buildSubgraphSchema([{ typeDefs, resolvers }]),
});

startStandaloneServer(server, {
  listen: { port: 4002 },
}).then(() => {
  console.log('✅ Hotel subgraph ready at http://localhost:4002/');
});
