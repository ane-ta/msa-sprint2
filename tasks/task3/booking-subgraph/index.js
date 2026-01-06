import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import gql from 'graphql-tag';

const typeDefs = gql`
  extend type Hotel @key(fields: "id") {
    id: ID! @external
  }
  type Booking @key(fields: "id") {
    id: ID!
    userId: String!
    hotelId: String!
    promoCode: String
    discountPercent: Int

    hotel: Hotel
  }

  type Query {
    bookingsByUser(userId: String!): [Booking]
  }

`;

const mockBookings = [
  { id: "b1", userId: "user1", hotelId: "h1", discountPercent: 10 },
  { id: "b2", userId: "user1", hotelId: "h2", discountPercent: 0 },
  { id: "b3", userId: "user2", hotelId: "h1", discountPercent: 20 },
];

const resolvers = {
  Query: {
    bookingsByUser: async (_, { userId }, context) => {
		// TODO: Реальный вызов к grpc booking-сервису или заглушка + ACL
        const headers = context.req.headers;

        if (!headers || headers['userid'] !== userId) {
          console.log(`ACL FAILED for user: ${userId}`);
          return [];
        }
        
        console.log(`ACL PASSED for user: ${userId}. Proceeding with gRPC call.`);
        return mockBookings.filter(b => b.userId === userId);
    },
  },
  Booking: {
    hotel: (bookging) =>{
        return { __typename: "Hotel", id: bookging.hotelId };
    }
  },
};

const server = new ApolloServer({
  schema: buildSubgraphSchema([{ typeDefs, resolvers }]),
});

startStandaloneServer(server, {
  listen: { port: 4001 },
  context: async ({ req }) => ({ req }),
}).then(() => {
  console.log('✅ Booking subgraph ready at http://localhost:4001/');
});
