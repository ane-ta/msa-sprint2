import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import gql from 'graphql-tag';
import { GraphQLError } from 'graphql'; 

const typeDefs = gql`
extend schema
  @link(url: "https://specs.apollo.dev/federation/v2.7",
        import: ["@key", "@shareable", "@inaccessible", "@override", "@requires", "@external"])

  extend type Hotel @key(fields: "id") {
    id: ID! @external
  }
  type Booking @key(fields: "id") {
    id: ID!
    userId: String!
    hotelId: String!

    promoCode: String
    discountPercent: Int!

    hotel: Hotel
  }

  type Query {
    bookingsByUser(userId: String!): [Booking]
  }

`;

const mockBookings = [
  { id: "b1", userId: "user1", hotelId: "h1", promoCode:"SUMMER_10", discountPercent: 10 },
  { id: "b2", userId: "user1", hotelId: "h2", discountPercent: 0 },
  { id: "b3", userId: "user2", hotelId: "h1", promoCode:"WINTER_20",discountPercent: 20 },
];

const resolvers = {
  Query: {
    bookingsByUser: async (_, { userId }, context) => {
		// TODO: Реальный вызов к grpc booking-сервису или заглушка + ACL
        const authenticatedUserId = context.req.headers['userid'];

        // 2. Проверяем наличие заголовка
        if (!authenticatedUserId) {
            console.error("Authenticated user ID header missing!");
             throw new GraphQLError('Authentication required.', {
                extensions: {
                    code: 'UNAUTHENTICATED',
                    http: { status: 401 },
                },
            });
        }
        
        // 3. Сравниваем ID пользователя из запроса с подтвержденным ID
        if (authenticatedUserId !== userId) {
          console.log(`ACL FAILED: User ${authenticatedUserId} attempted to access bookings for user ${userId}`);
          
          // Выбрасываем ошибку доступа (Forbidden)
          throw new GraphQLError('You are not authorized to view bookings for this user.', {
            extensions: {
                code: 'FORBIDDEN',
                http: { status: 403 },
            },
          });

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
